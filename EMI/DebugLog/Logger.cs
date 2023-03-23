using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.DebugLog
{
    /// <summary>
    /// Для вывода отладочной информации
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Вызывается когда EMI логирует сообщение
        /// </summary>
        /// <param name="client">клиент который вызвал сообщение или null если сервер</param>
        /// <param name="type">тип сообщения</param>
        /// <param name="time">время когда было сгенерировано сообщение</param>
        /// <param name="message">текст сообщения</param>
        public delegate void Message(Client client, LogType type, DateTime time, string message);
        /// <summary>
        /// Вызывается при создании сообщения
        /// </summary>
        public event Message OnMessage;

        internal Logger()
        {

        }

        private static string Trace()
        {
#if DebugPro
            return $"\nDebugTrace: {DebugUtil.GetStackTrace()}";
#else
            return "";
#endif
        }

        internal void Log(LogMessage message,params object[] format)
        {
#if DebugPro || DEBUG
            string msg = string.Format(message.Message, format);
            Console.WriteLine($"EMI => {message.Type} => {msg}");
            OnMessage?.Invoke(null, message.Type, DateTime.Now, msg);
#endif
        }

        internal void Log(Client client, LogMessage message, params object[] format)
        {
#if DebugPro || DEBUG
            string msg = string.Format(message.Message, format);
            Console.WriteLine($"EMI => {message.Type} => client: {client.RemoteAddress} => {msg}");
            OnMessage?.Invoke(client, message.Type, DateTime.Now, msg);
#endif
        }
    }
}
