using System;
using System.Collections.Generic;

namespace EMI.DebugServer
{
    using NetworkData;

    public class EDebuggerClient
    {
        private static readonly Guid ClientGuid = Guid.NewGuid();
        private readonly List<Client> Clients = new List<Client>();
        private readonly Server DebugServer;
        private readonly Client TargetClient;

        public EDebuggerClient(Client client, Network.INetworkService service)
        {
            DebugServer = new Server(service);
            TargetClient = client;

            DebugServer.RPC.RegisterMethod(() => new ClientInfo(TargetClient, ClientGuid), EDebuggerClientRC.GetClientInfo);
            DebugServer.RPC.RegisterMethod(() => { var n = new NGCInfo(); n.Create(); return n; }, EDebuggerBothRC.GetNGCInfo);

            TargetClient.Logger.OnMessage += Logger_OnMessage;
        }

        private void Logger_OnMessage(Client client, DebugLog.LogType type, DateTime time, string message)
        {
            if (!DebugServer.IsRun)
                return;

            MSG msg = new MSG()
            {
                IsServerMSG = client == null,
                ClientID = ClientGuid,
                Type = type,
                Time = time,
                Message = message,
            };

            RunAll((cc) =>
            {
                _ = EDebuggerBothRC.OnMSG.RCall(msg, cc);
            });
        }


        private void RunAll(Action<Client> action)
        {
            lock (Clients)
                foreach (var cc in Clients)
                {
                    action.Invoke(cc);
                }
        }

        public void Start(string address)
        {
            DebugServer.Start(address);

            AcceptTask();
        }

        public void Stop()
        {
            DebugServer.Stop();
        }

        private async void AcceptTask()
        {
            while (DebugServer.IsRun)
            {
                var client = await DebugServer.Accept();
                if (client != null)
                {
                    try
                    {
                        await EDebuggerBothRC.InitDebugger.RCall(DebuggerType.Client, client, RCType.ReturnWait);

                        lock (this)
                        {
                            lock (Clients)
                                Clients.Add(client);

                            client.Disconnected += (_) =>
                            {
                                lock (Clients)
                                    Clients.Remove(client);
                            };

                            AllInfoClient info = new AllInfoClient(new ClientInfo(TargetClient, ClientGuid), new RPCInfo(TargetClient.RPC, false, ClientGuid));
                            _ = EDebuggerClientRC.OnSendAllInfo.RCall(info, client);

                            void SendRPCInfo()
                            {
                                if (client.IsConnect)
                                    _ = EDebuggerClientRC.OnSendRPCInfo.RCall(new RPCInfo(TargetClient.RPC, false, ClientGuid), client);
                            }

                            TargetClient.RPC.OnChangedRegisteredMethods += SendRPCInfo;
                            TargetClient.RPC.OnChangedRegisteredMethodsForwarding += SendRPCInfo;

                            client.Disconnected += (_) =>
                            {
                                lock (Clients)
                                    Clients.Remove(client);

                                TargetClient.RPC.OnChangedRegisteredMethods -= SendRPCInfo;
                                TargetClient.RPC.OnChangedRegisteredMethodsForwarding -= SendRPCInfo;
                            };
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}
