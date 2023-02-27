using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace EMI.LogMessages
{
    internal static class Messages
    {
        private static Dictionary<ushort, string> Map = Init();

        public static string Log(ushort id)
        {
#if DEBUG
            return $"{id:000} # {Map[id]}\nDebugTrace: {DebugUtil.GetStackTrace()}";
#else
            return $"{id:000} # {Map[id]}";
#endif
        }

        public static string Log(ushort id,params object[] args)
        {
#if DEBUG
            return $"{string.Format(Map[id],args)}\nDebugTrace: {DebugUtil.GetStackTrace()}";
#else
            return $"{id:000} # {string.Format(Map[id],args)}";
#endif
        }

        private static Dictionary<ushort, string> Init()
        {
            var map  = new Dictionary<ushort, string>();
            var dat = Properties.Resources.Messages;

            string[] lines = dat.Split('\n');
            foreach(var line in lines)
            {
                if (line.Length < 5)
                    continue;
                try
                {
                    int i = line.IndexOf('#');
                    string id = line.Substring(0, i).Trim();
                    string message = line.Substring(i + 1).Trim();

                    map.Add(ushort.Parse(id), message);
                }
                catch(Exception _) { }
            }

            return map;
        }
    }
}
