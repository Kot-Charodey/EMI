using System;
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

        public bool IsConnect => TcpClient.Connected;

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

        internal NetBaseTCPClient(NetBaseTCPServer server, TcpClient tcpClient)
        {
            ProArrayBuffer = server.ProArrayBuffer;
            TcpClient = tcpClient;
            NetworkStream = TcpClient.GetStream();
            Server = server;
            Init();
        }

        public NetBaseTCPClient()
        {
            TcpClient = new TcpClient();
            Init();
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

        //вернёт false если была произведенна отмена
        private async Task<bool> AcceptLow(byte[] buffer, int count, CancellationToken token)
        {
            while (count > 0 && !token.IsCancellationRequested)
            {
                count -= await NetworkStream.ReadAsync(buffer, 0, count, token).ConfigureAwait(false);
            }
            return count == 0;
        }

        public async Task<Array2Offser> AcceptAsync(CancellationToken token)
        {
            await SemaphoreRead.WaitAsync(token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
                throw new ClientDisconnectException();

            bool status = await AcceptLow(AcceptHeaderBuffer, DataGramInfo.SizeOf, token).ConfigureAwait(false);
            if (status == false)
                throw new ClientDisconnectException();

            DataGramInfo header;
            unsafe
            {
                fixed (byte* headerBufferPtr = &AcceptHeaderBuffer[0])
                {
                    header = *((DataGramInfo*)headerBufferPtr);
                }
            }

            if (header.GetIsDisconnectMessage())//пришло сообщение что мы должны отключиться
            {
                byte[] messageByte = new byte[header.GetSize()];
                await AcceptLow(messageByte, messageByte.Length, default).ConfigureAwait(false);
                Disconnected?.Invoke(Encoding.Default.GetString(messageByte));
                TcpClient.Close();
                throw new ClientDisconnectException();
            }
            else
            {
                var array = await ProArrayBuffer.AllocateArrayAsync(header.GetSize(), token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                status = await AcceptLow(array.Bytes, array.Length, token).ConfigureAwait(false);
                SemaphoreRead.Release();
                if (!status)
                {
                    return default;
                }
                return new Array2Offser(array, 0);
            }
        }

        public void Disconnect(string user_error)
        {
            if (IsServerSide)
            {
                lock (Server.TCPClients)
                    Server.TCPClients.Remove(this);
            }

            Task.Run(async () =>
            {
                if (TcpClient.Connected)
                {
                    try //отправляет ошибку удалённому клиенту и разрывает соединение
                    {
                        byte[] bytes = Encoding.Unicode.GetBytes(user_error);
                        WrapperArray wa = new WrapperArray(bytes);
                        await SendAsync(wa, true, new CancellationTokenSource(2000).Token).ConfigureAwait(false);
                        await Task.Delay(2000).ConfigureAwait(false);
                    }
                    catch { }

                    TcpClient.Close();
                }
            });

            Disconnected?.Invoke(user_error);
        }
        
        public void Send(IReleasableArray array, bool guaranteed)
        {
            SemaphoreWrite.Wait();
            
            DataGramInfo header = new DataGramInfo(array.Length, false);

            unsafe
            {
                fixed (byte* headerBufferPtr = &AcceptHeaderBuffer[0])
                {
                    *((DataGramInfo*)headerBufferPtr) = header;
                }
            }

            NetworkStream.Write(AcceptHeaderBuffer, 0, AcceptHeaderBuffer.Length);

            for (int i = 0; i < array.Length; i += MaxOneSendSize)
            {
                NetworkStream.Write(array.Bytes, i, Math.Min(array.Length - i, MaxOneSendSize));
            }
            SemaphoreWrite.Release();
        }

        public async Task SendAsync(IReleasableArray array, bool guaranteed, CancellationToken token)
        {
            await SemaphoreWrite.WaitAsync(token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
                return;

            DataGramInfo header = new DataGramInfo(array.Length, false);

            unsafe
            {
                fixed (byte* headerBufferPtr = &AcceptHeaderBuffer[0])
                {
                    *((DataGramInfo*)headerBufferPtr) = header;
                }
            }

            await NetworkStream.WriteAsync(SendHeaderBuffer, 0, SendHeaderBuffer.Length).ConfigureAwait(false);

            for (int i = 0; i < array.Length; i += MaxOneSendSize)
            {
                await NetworkStream.WriteAsync(array.Bytes, i, Math.Min(array.Length - i, MaxOneSendSize)).ConfigureAwait(false);
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
                    return true;
                }
                catch (Exception e)
                {
                    Disconnected?.Invoke(e.ToString());
                    return false;
                }
            }
        }
    }
}