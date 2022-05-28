using System;
using System.Collections.Generic;
using System.Reflection;

namespace EMI.SyncInterface
{
    internal struct InterfaceTypes
    {
        public Type Client;
        public List<FieldList> ClientFields;
        public Type Server;
        public List<FieldList> ServerFields;

        public InterfaceTypes(Type client, List<FieldList> cf, Type server, List<FieldList> sf)
        {
            Client = client;
            ClientFields = cf;
            Server = server;
            ServerFields = sf;
        }
    }
}