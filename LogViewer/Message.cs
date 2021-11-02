using System;
using System.Diagnostics;
using EMI.Debug;

namespace LogViewer
{
    public struct Message
    {
        public string Text;
        public DateTime TimePC;
        public TimeSpan TimeRecord;
        public Net.LogTag Tag;
        public Net.LogType Type;

        public Message(byte[] rawData, Stopwatch stopwatch, long offset = 0)
        {
            TimeRecord = stopwatch.Elapsed;
            Net.PackagerLog.UnPack(rawData, offset, out Type, out Tag, out TimePC, out Text);
        }

        public static Message CreateMSG(string MSG,Net.LogTag tag, Stopwatch stopwatch)
        {
            return new Message()
            {
                Text = MSG,
                TimePC = DateTime.Now,
                TimeRecord = stopwatch.Elapsed,
                Tag = tag,
                Type = Net.LogType.ExternalMessage
            };
        }
    }
}