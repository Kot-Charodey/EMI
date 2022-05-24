using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI
{
    internal static class DebugUtil
    {
        public static string GetStackTrace()
        {
            var st = new System.Diagnostics.StackTrace(true);
            string err = "Stack trace:";
            for (int i = 1; i < st.FrameCount; i++)
            {
                var frame = st.GetFrame(i);
                if (frame.GetFileLineNumber() == 0)
                    break;
                err += $"\n{frame.GetFileName()} {frame.GetMethod().Name} at {frame.GetFileLineNumber()} line";
            }
            return err;
        }
    }
}