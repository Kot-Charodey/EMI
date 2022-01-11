﻿using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

using EMI.Network;
using EMI.ProBuffer;
using EMI.MyException;

namespace NetBaseTCP
{
    public class NetBaseTCPClient : INetworkClient
    {
        public ProArrayBuffer ProArrayBuffer { get; set; }

        public bool IsConnect { get; private set; }

        public event INetworkClientDisconnected Disconnected;

        private NetworkStream NetworkStream;
        private readonly TcpClient TcpClient;
        private readonly NetBaseTCPServer Server;
        private const int MaxOneSendSize = 1024;
        private bool IsServerSide => Server != null;

        private readonly byte[] AcceptHeaderBuffer = new byte[DataGramInfo.SizeOf];

        private readonly byte[] SendHeaderBuffer = new byte[DataGramInfo.SizeOf];


        private SemaphoreSlim SemaphoreRead;
        private SemaphoreSlim SemaphoreWrite;

        /// <summary>
        /// On server side
        /// </summary>
        /// <param name="server"></param>
        /// <param name="tcpClient"></param>
        internal NetBaseTCPClient(NetBaseTCPServer server, TcpClient tcpClient)
        {
            lock (this)
            {
                ProArrayBuffer = server.ProArrayBuffer;
                TcpClient = tcpClient;
                NetworkStream = TcpClient.GetStream();
                Server = server;
                Init();
                IsConnect = true;
            }
        }

        public NetBaseTCPClient()
        {
            lock (this)
            {
                TcpClient = new TcpClient();
                Init();
            }
        }

        private void Init()
        {
            SemaphoreRead = new SemaphoreSlim(1, 1);
            SemaphoreWrite = new SemaphoreSlim(1, 1);
            TcpClient.NoDelay = true;
            TcpClient.ReceiveTimeout = 15;
            TcpClient.SendTimeout = 15;

            Disconnected += NetBaseTCPClient_Disconnected;
        }

        private void NetBaseTCPClient_Disconnected(string error)
        {
            SemaphoreRead.Dispose();
            SemaphoreWrite.Dispose();

            if (!IsServerSide)
            {
                SemaphoreRead = new SemaphoreSlim(1, 1);
                SemaphoreWrite = new SemaphoreSlim(1, 1);
            }
        }

        private async Task AcceptLow(byte[] buffer, int count, CancellationToken token)
        {
            try
            {
                while (count > 0)
                {
                    count -= await NetworkStream.ReadAsync(buffer, 0, count, token).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Disconnect(e.Message);
            }
        }

        public async Task<IReleasableArray> AcceptAsync(CancellationToken token)
        {
            await SemaphoreRead.WaitAsync(token).ConfigureAwait(false);

            await AcceptLow(AcceptHeaderBuffer, DataGramInfo.SizeOf, token).ConfigureAwait(false);

            DataGramInfo header;
            unsafe
            {
                fixed (byte* headerBufferPtr = &AcceptHeaderBuffer[0])
                {
                    header = *((DataGramInfo*)headerBufferPtr);
                }
            }

            var array = await ProArrayBuffer.AllocateArrayAsync(header.GetSize(), token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            await AcceptLow(array.Bytes, array.Length, token).ConfigureAwait(false);
            SemaphoreRead.Release();
            return array;
        }

        public void Disconnect(string user_error)
        {
            lock (this)
            {
                if (IsConnect)
                {
                    IsConnect = false;

                    if (IsServerSide)
                    {
                        lock (Server.TCPClients)
                            Server.TCPClients.Remove(this);
                    }

                    try { TcpClient.Close(); } catch { }

                    Disconnected?.Invoke(user_error);
                }
            }
        }

        public void Send(IReleasableArray array, bool guaranteed)
        {
            SemaphoreWrite.Wait();
            try
            {
                DataGramInfo header = new DataGramInfo(array.Length, false);

                unsafe
                {
                    fixed (byte* headerBufferPtr = &SendHeaderBuffer[0])
                    {
                        *((DataGramInfo*)headerBufferPtr) = header;
                    }
                }

                NetworkStream.Write(SendHeaderBuffer, 0, SendHeaderBuffer.Length);

                for (int i = 0; i < array.Length; i += MaxOneSendSize)
                {
                    NetworkStream.Write(array.Bytes, i, Math.Min(array.Length - i, MaxOneSendSize));
                }
            }
            catch (Exception e)
            {
                Disconnect(e.Message);
            }
            SemaphoreWrite.Release();
        }

        public async Task SendAsync(IReleasableArray array, bool guaranteed, CancellationToken token)
        {
            await SemaphoreWrite.WaitAsync(token).ConfigureAwait(false);
            try
            {
                DataGramInfo header = new DataGramInfo(array.Length, false);

                unsafe
                {
                    fixed (byte* headerBufferPtr = &SendHeaderBuffer[0])
                    {
                        *((DataGramInfo*)headerBufferPtr) = header;
                    }
                }

                await NetworkStream.WriteAsync(SendHeaderBuffer, 0, SendHeaderBuffer.Length).ConfigureAwait(false);

                for (int i = 0; i < array.Length; i += MaxOneSendSize)
                {
                    await NetworkStream.WriteAsync(array.Bytes, i, Math.Min(array.Length - i, MaxOneSendSize)).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Disconnect(e.Message);
            }
            SemaphoreWrite.Release();
        }

        public async Task<bool> Сonnect(string address, CancellationToken token)
        {
            if (IsServerSide)
            {
                throw new NotSupportedException();
            }
            else
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                token.Register(() => cts.Cancel());
                try
                {
                    bool wait = await EMI.TaskUtilities.InvokeAsync(() =>
                    {
                        TcpClient.Connect(Utilities.ParseAddress(address));
                    }, cts).ConfigureAwait(false);

                    NetworkStream = TcpClient.GetStream();
                    IsConnect = true;
                    return true;
                }
                catch (Exception e)
                {
                    try { TcpClient.Close(); } catch { }
                    IsConnect = false;
                    Disconnected?.Invoke(e.ToString());
                    return false;
                }
            }
        }
    }
}