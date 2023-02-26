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
        internal void Log(LogType type, string message)
        {
#if DebugPro || DEBUG
            OnMessage?.Invoke(null, type, DateTime.Now, message);
#endif
        }

        internal void Log(Client client, LogType type, string message)
        {
#if DebugPro || DEBUG
            OnMessage?.Invoke(client, type, DateTime.Now, message);
#endif
        }
    }
}
