using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.DebugServer.NetworkData
{
    public struct MSG
    {
        public bool IsServerMSG;
        public Guid ClientID;
        public DebugLog.LogType Type;
        public DateTime Time;
        public string Message;
    }
}
