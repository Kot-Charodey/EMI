using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.DebugLog
{
    /// <summary>
    /// Сообщение об ошибке или событие
    /// </summary>
    internal struct LogMessage
    {
        /// <summary>
        /// Уровень критичности сообщения
        /// </summary>
        public LogType Type;
        /// <summary>
        /// Сообщение об ошибке (поддерживает string format)
        /// </summary>
        public string Message;

        /// <summary>
        /// Создать новое сообщение
        /// </summary>
        /// <param name="type">уровень критичности сообщения</param>
        /// <param name="message">сообщение об ошибке (поддерживает string format)</param>
        /// <returns></returns>
        public static LogMessage Create(LogType type, string message)
        {
            var msg = new LogMessage()
            {
                Type = type,
                Message = message
            };

            return msg;
        }

        public string Format(params object[] args)
        {
            return string.Format(Message, args);
        }
    }
}
