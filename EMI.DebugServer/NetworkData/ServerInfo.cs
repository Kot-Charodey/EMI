﻿namespace EMI.DebugServer.NetworkData
{
    public struct ServerInfo
    {
        public string Address;
        public string ServiceName;
        public bool IsRun;

        public ServerInfo(Server server)
        {
            Address = server.Address;
            ServiceName = server.ServiceName.ToString();
            IsRun = server.IsRun;
        }
    }
}
