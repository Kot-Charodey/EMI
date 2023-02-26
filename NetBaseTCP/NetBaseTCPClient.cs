using EMI.MyException;
using EMI.Network;
using EMI.NGC;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetBaseTCP
{
    public class NetBaseTCPClient : INetworkClient
    {
        public bool IsConnect
        {
            get;
            private set;
        }

        public event INetworkClientDisconnected Disconnected;

        private NetworkStream NetworkStream;
        private TcpClient TcpClient;
        private readonly NetBaseTCPServer Server;
        private const int MaxOneSendSize = 1024;
        private bool IsServerSide => Server != null;

        private readonly byte[] AcceptHeaderBuffer = new byte[MessageHeader.SizeOf];
        private readonly byte[] SendHeaderBuffer = new byte[MessageHeader.SizeOf];


        private SemaphoreSlim SemaphoreRead;
        private SemaphoreSlim SemaphoreWrite;

        /// <summary>
        /// Инициализация серверного клиента
        /// </summary>
        /// <param name="server"></param>
        /// <param name="tcpClient"></param>
        internal NetBaseTCPClient(NetBaseTCPServer server, TcpClient tcpClient)
        {
            lock (this)
            {
                TcpClient = tcpClient;
                NetworkStream = TcpClient.GetStream();
                Server = server;
                Init();
                Disconnected += NetBaseTCPClient_Disconnected;
                IsConnect = true;
            }
        }

        /// <summary>
        /// Инициализация клинта
        /// </summary>
        public NetBaseTCPClient()
        {
            Disconnected += NetBaseTCPClient_Disconnected;
        }

        private void Init()
        {
            SemaphoreRead = new SemaphoreSlim(1, 1);
            SemaphoreWrite = new SemaphoreSlim(1, 1);
            TcpClient.NoDelay = true;
            TcpClient.ReceiveTimeout = 15;
            TcpClient.SendTimeout = 60000;
            TcpClient.ReceiveBufferSize = MaxOneSendSize * 20;
            TcpClient.SendBufferSize = MaxOneSendSize;
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
            int offset = 0;
            while (count > 0)
            {
                int size = await NetworkStream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
                count -= size;
                offset += size;
            }
        }

        public void Disconnect(string user_error)
        {
            //System.Diagnostics.Debug.WriteLine("Disconnect: " + user_error);

            if (IsConnect)
            {
                IsConnect = false;

                if (IsServerSide)
                {
                    lock (Server.TCPClients)
                        Server.TCPClients.Remove(this);
                }
                Task.Run(async () =>
                {
                    try
                    {
                        await SendLow(new EasyArray(
                            System.Text.Encoding.Default.GetBytes(user_error)),
                            true, new CancellationTokenSource(1000).Token);
                        await Task.Delay(1500);
                    }
                    finally
                    {
                        try
                        {
                            TcpClient.Close();
                        }
                        catch { }
                    }
                });
                Disconnected?.Invoke(user_error);
            }
        }

        public async Task<bool> Сonnect(string address, CancellationToken token)
        {
            if (IsServerSide)
            {
                throw new NotSupportedException();
            }
            else if (IsConnect)
            {
                throw new AlreadyException("Client is already connected!");
            }
            else
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                token.Register(() => cts.Cancel());
                try
                {
                    TcpClient = new TcpClient();
                    Init();

                    bool wait = await EMI.TaskUtilities.InvokeAsync(() =>
                    {
                        TcpClient.Connect(Utilities.ParseIPAddress(address));
                    }, cts).ConfigureAwait(false);
                    if (wait == false || token.IsCancellationRequested)
                    {
                        try { TcpClient.Close(); } catch { }
                        IsConnect = false;
                        return false;
                    }
                    else
                    {
                        NetworkStream = TcpClient.GetStream();
                        IsConnect = true;
                        return true;
                    }
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

        private async Task SendLow(INGCArray array, bool isError, CancellationToken token)
        {
            await SemaphoreWrite.WaitAsync(token);
            try
            {
                MessageHeader header = new MessageHeader(array.Length, isError);
                header.WriteToBuffer(SendHeaderBuffer);
                await NetworkStream.WriteAsync(SendHeaderBuffer, 0, SendHeaderBuffer.Length, token);

                if (token.IsCancellationRequested)
                    return;

                for (int i = 0; i < array.Length && !token.IsCancellationRequested; i += MaxOneSendSize)
                {
                    await NetworkStream.WriteAsync(array.Bytes, i, Math.Min(array.Length - i, MaxOneSendSize), token).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Disconnect(e.Message);
            }
            finally
            {
                try
                {
                    SemaphoreWrite.Release();
                }
                catch { }
            }
        }

        public async Task Send(INGCArray array, bool guaranteed, CancellationToken token)
        {
            await SendLow(array, false, token);
        }

        public async Task<INGCArray> AcceptPacket(int max_size, CancellationToken token)
        {
            await SemaphoreRead.WaitAsync(token);
            try
            {
                await AcceptLow(AcceptHeaderBuffer, MessageHeader.SizeOf, token).ConfigureAwait(false);
                if (token.IsCancellationRequested)
                    return INGCArrayUtils.EmptyArray;

                var header = MessageHeader.FromBytes(AcceptHeaderBuffer);
                int size = header.GetSize();
                bool isDisconnectMessage = header.IsDisconnectMessage();
                if (size > max_size && !isDisconnectMessage || isDisconnectMessage && size > 4098)
                {
                    if (isDisconnectMessage)
                        throw new ClientViolationRightsException($"The error message is too large!");
                    else
                        throw new ClientViolationRightsException($"The allowed buffer ({size} > {max_size}) for reading an incoming packet has been exceeded!");
                }
                else
                {
                    INGCArray array = new NGCArray(size);
                    try
                    {
                        await AcceptLow(array.Bytes, array.Length, token).ConfigureAwait(false);

                        if (token.IsCancellationRequested)
                        {
                            return INGCArrayUtils.EmptyArray;
                        }
                        else
                        {

                            if (isDisconnectMessage)
                            {
                                string message = System.Text.Encoding.Default.GetString(array.Bytes, array.Offset, array.Length);
                                throw new ClientDisconnectException($"The remote client broke the connection by sending a message - {message}");
                            }
                            else
                            {
                                return array;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        array.Dispose();
                        Disconnect(e.Message);
                        return INGCArrayUtils.EmptyArray;
                    }
                }
            }
            catch (Exception e)
            {
                Disconnect(e.Message);
                return INGCArrayUtils.EmptyArray;
            }
            finally
            {
                try
                {
                    SemaphoreRead.Release();
                }
                catch { }
            }
        }

        public string GetRemoteClientAddress()
        {
            try
            {
                var addr = TcpClient.Client.RemoteEndPoint as IPEndPoint;
                return $"{addr.Address}#{addr.Port}";
            }
            catch
            {
                return "none";
            }
        }
    }
}