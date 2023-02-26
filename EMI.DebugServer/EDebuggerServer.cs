using System;
using System.Collections.Generic;
using System.Linq;

namespace EMI.DebugServer
{
    using NetworkData;

    public class EDebuggerServer
    {
        private readonly List<Client> Clients = new List<Client>();
        private readonly Dictionary<Client, Guid> ClientGuids = new Dictionary<Client, Guid>();
        private readonly Server DebugServer;
        private readonly Server TargetServer;

        public EDebuggerServer(Server server, Network.INetworkService service)
        {
            DebugServer = new Server(service);
            TargetServer = server;

            TargetServer.OnClientConnect += AddClient;
            TargetServer.OnClientDisconnect += RemoveClient;

            DebugServer.RPC.RegisterMethod(() => { return new ServerInfo(TargetServer); }, EDebuggerServerRC.GetServerInfo);
            DebugServer.RPC.RegisterMethod(GetClientInfos, EDebuggerServerRC.GetClientInfos);
            DebugServer.RPC.RegisterMethod(() => { var n = new NGCInfo(); n.Create(); return n; }, EDebuggerBothRC.GetNGCInfo);

            TargetServer.Logger.OnMessage += Logger_OnMessage;
        }

        private void Logger_OnMessage(Client client, DebugLog.LogType type, DateTime time, string message)
        {
            if (!DebugServer.IsRun)
                return;

            MSG msg = new MSG()
            {
                IsServerMSG = client == null,
                Type = type,
                Time = time,
                Message = message,
            };

            if (client != null)
            {
                if (ClientGuids.ContainsKey(client))
                {
                    msg.ClientID = ClientGuids[client];
                }
                else
                {
                    lock (ClientGuids)
                    {
                        Guid guid = Guid.NewGuid();
                        msg.ClientID = guid;
                        ClientGuids.Add(client, guid);
                    }
                }
            }
                

            RunAll((cc) =>
            {
                _ = EDebuggerBothRC.OnMSG.RCall(msg, cc);
            });
        }

        public void Start(string address)
        {
            DebugServer.Start(address);

            ClientGuids.Clear();
            foreach (var cc in TargetServer.ServerClients)
            {
                AddClient(cc);
            }

            AcceptTask();
        }

        private async void AcceptTask()
        {
            while (DebugServer.IsRun)
            {
                var client = await DebugServer.Accept();
                if (client != null)
                {
                    await EDebuggerBothRC.InitDebugger.RCall(DebuggerType.Server, client, RCType.ReturnWait);
                    lock (this)
                    {
                        lock (Clients)
                            Clients.Add(client);

                        AllInfo info = new AllInfo(new ServerInfo(TargetServer), GetClientInfos(), GetRPCInfos());
                        _ = EDebuggerServerRC.OnSendAllInfo.RCall(info, client);

                        void SendRPCInfo()
                        {
                            if(client.IsConnect)
                            _ = EDebuggerServerRC.OnSendRPCInfo.RCall(GetRPCInfos(), client);
                        }

                        TargetServer.RPC.OnChangedRegisteredMethods += SendRPCInfo;
                        TargetServer.RPC.OnChangedRegisteredMethodsForwarding += SendRPCInfo;

                        client.Disconnected += (_) =>
                        {
                            lock (Clients)
                                Clients.Remove(client);

                            TargetServer.RPC.OnChangedRegisteredMethods -= SendRPCInfo;
                            TargetServer.RPC.OnChangedRegisteredMethodsForwarding -= SendRPCInfo;
                        };
                    }
                }
            }
        }

        private ClientInfo[] GetClientInfos()
        {
            var info = from client in TargetServer.ServerClients 
                       select new ClientInfo(client, ClientGuids[client]);
            return info.ToArray();
        }

        private RPCInfo[] GetRPCInfos()
        {
            List<RPCInfo> info = new List<RPCInfo>(
                from cc in TargetServer.ServerClients
                select new RPCInfo(cc.RPC, false, ClientGuids[cc]))
            {
                new RPCInfo(TargetServer.RPC, true, default)
            };
            return info.ToArray();
        }

        private void RunAll(Action<Client> action)
        {
            lock (Clients)
                foreach (var cc in Clients)
                {
                    action.Invoke(cc);
                }
        }

        private void RemoveClient(Client obj)
        {
            if (!DebugServer.IsRun)
                return;

            lock (this)
                ClientGuids.Remove(obj);
        }

        private void AddClient(Client obj)
        {
            if (!DebugServer.IsRun)
                return;

            lock (this)
            {
                if(!ClientGuids.ContainsKey(obj))
                    ClientGuids.Add(obj, Guid.NewGuid());
            }

            void SendRPCInfo()
            {
                _ = EDebuggerServerRC.OnSendRPCInfo.RCall(GetRPCInfos(), obj);
            }

            obj.LocalRPC.OnChangedRegisteredMethods += SendRPCInfo;
            obj.LocalRPC.OnChangedRegisteredMethodsForwarding += SendRPCInfo;
        }

        public void Stop()
        {
            DebugServer.Stop();
        }
    }
}
