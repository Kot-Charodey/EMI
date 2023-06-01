using EMI.MyException;
using EMI.Network;
using EMI.NGC;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EMI.NetParallelTCP
{
    public class NetParallelTCPClient : INetworkClient
    {
        public bool IsConnect
        {
            get;
            private set;
        }

        public event INetworkClientDisconnected Disconnected;

        private NetworkStream NetworkStream;
        private TcpClient TcpClient;
        private readonly NetParallelTCPServer Server;
        private const int MaxOneSendSize = 1024;
        private bool IsServerSide => Server != null;
        private ushort IDGen = 0;

        private static readonly int MaxHeaderSize = Math.Max(MessageHeader.SizeOf, MessagePacketSegment.SizeOf);
        private readonly Dictionary<ushort, Packet> PacketBuffer = new Dictionary<ushort, Packet>();


        private SemaphoreSlim SemaphoreRead;
        private SemaphoreSlim SemaphoreWrite;

        /// <summary>
        /// Инициализация серверного клиента
        /// </summary>
        /// <param name="server"></param>
        /// <param name="tcpClient"></param>
        internal NetParallelTCPClient(NetParallelTCPServer server, TcpClient tcpClient)
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
        public NetParallelTCPClient()
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

        private async Task AcceptLow(byte[] buffer, int offset, int count, CancellationToken token)
        {
            while (count > 0)
            {
                int size = await NetworkStream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
                count -= size;
                offset += size;
            }
        }

        public void Disconnect(string user_error)
        {
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
            try
            {
                MessageHeader header;
                bool big_packet = array.Length > MaxOneSendSize;
                try
                {
                    await SemaphoreWrite.WaitAsync(token).ConfigureAwait(false);

                    if (token.IsCancellationRequested)
                        return;

                    header = new MessageHeader(IDGen++, array.Length, isError);
                    using (var sendHeaderBuffer = new NGCArray(MessageHeader.SizeOf))
                    {
                        header.WriteToBuffer(sendHeaderBuffer.Bytes);
                        await NetworkStream.WriteAsync(sendHeaderBuffer.Bytes, 0, sendHeaderBuffer.Length, token).ConfigureAwait(false);
                    }

                    if (!big_packet)
                    {
                        await NetworkStream.WriteAsync(array.Bytes, 0, array.Length, token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    SemaphoreWrite.Release();
                }

                if (array.Length > MaxOneSendSize)
                {
                    var mps = new MessagePacketSegment(header.ID);
                    using (var sendMPSBuffer = new NGCArray(MessagePacketSegment.SizeOf))
                    {
                        mps.WriteToBuffer(sendMPSBuffer.Bytes);

                        for (int i = 0; i < array.Length && !token.IsCancellationRequested; i += MaxOneSendSize)
                        {
                            try
                            {
                                await SemaphoreWrite.WaitAsync(token).ConfigureAwait(false);
                                
                                await NetworkStream.WriteAsync(sendMPSBuffer.Bytes, 0, sendMPSBuffer.Length, token);
                                await NetworkStream.WriteAsync(array.Bytes, i, Math.Min(array.Length - i, MaxOneSendSize), token).ConfigureAwait(false);
                            }
                            finally
                            {
                                SemaphoreWrite.Release();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Disconnect(e.Message);
            }
        }

        public async Task Send(INGCArray array, bool guaranteed, CancellationToken token)
        {
            if (guaranteed == true)
            {
                await SendLow(array, false, token);
            }
            else
            {
                var cts = new CancellationTokenSource(5000);
                token.Register(() =>
                {
                    if (cts != null && !cts.IsCancellationRequested)
                    {
                        cts.Cancel();
                    }
                });
                await SendLow(array, false, cts.Token);
            }
        }

        public async Task<INGCArray> AcceptPacket(int max_size, CancellationToken token)
        {
            try
            {
                while (true)
                {
                    await SemaphoreRead.WaitAsync(token).ConfigureAwait(false);
                    try 
                    {
                        using (var headerBuffer = new NGCArray(MaxHeaderSize))
                        {
                            await AcceptLow(headerBuffer.Bytes, 0, 1, token).ConfigureAwait(false);

                            if (token.IsCancellationRequested)
                                return INGCArrayUtils.EmptyArray;

                            MessageType type = (MessageType)headerBuffer.Bytes[0];

                            switch (type)
                            {
                                case MessageType.MessageHeader:
                                    await AcceptLow(headerBuffer.Bytes, 1, MessageHeader.SizeOf - 1, token).ConfigureAwait(false);

                                    if (token.IsCancellationRequested)
                                        return INGCArrayUtils.EmptyArray;

                                    var header = MessageHeader.FromBytes(headerBuffer.Bytes);
                                    int size = header.GetSize();
                                    bool isDisconnectMessage = header.IsDisconnectMessage();
                                    if (size > max_size && !isDisconnectMessage || isDisconnectMessage && size > 4098)
                                    {
                                        if (isDisconnectMessage)
                                            throw new ClientViolationRightsException($"The error message is too large!");
                                        else
                                            throw new ClientViolationRightsException($"The allowed buffer ({size} > {max_size}) for reading an incoming packet has been exceeded!");
                                    }

                                    if (size <= MaxOneSendSize)
                                    {
                                        INGCArray array = new NGCArray(size);

                                        try
                                        {
                                            await AcceptLow(array.Bytes, 0, array.Length, token).ConfigureAwait(false);
                                            
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
                                    else
                                    {
                                        var buffer = new NGCArray(header.GetSize());
                                        var packet = new Packet()
                                        {
                                            Buffer = buffer,
                                            Size = size,
                                        };
                                        PacketBuffer.Add(header.ID, packet);
                                    }

                                    break;
                                case MessageType.MessagePacketSegment:
                                    await AcceptLow(headerBuffer.Bytes, 1, MessagePacketSegment.SizeOf - 1, token).ConfigureAwait(false);

                                    if (token.IsCancellationRequested)
                                        return INGCArrayUtils.EmptyArray;

                                    var mps = MessagePacketSegment.FromBytes(headerBuffer.Bytes);

                                    var packetInfo = PacketBuffer[mps.ID];

                                    int readLen = Math.Min(MaxOneSendSize, packetInfo.Size - packetInfo.Offset);
                                    await AcceptLow(packetInfo.Buffer.Bytes, packetInfo.Offset, readLen, token).ConfigureAwait(false);
                                    packetInfo.Offset += readLen;

                                    if (packetInfo.Offset == packetInfo.Size)
                                    {
                                        PacketBuffer.Remove(mps.ID);
                                        return packetInfo.Buffer;
                                    }
                                    break;
                                default:
                                    throw new ClientDisconnectException($"incorrect message type");
                            }
                        }
                    }
                    finally
                    {
                        SemaphoreRead.Release();
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