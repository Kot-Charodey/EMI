using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI
{
    /// <summary>
    /// Тип удалённого вызова
    /// </summary>
    public enum RCType : byte
    {
        /// <summary>
        /// Вызвать используя не безопасный способ отправки (вызов может не произойти) (не ждёт завершение выполнения return null)
        /// </summary>
        Fast,
        /// <summary>
        /// Вызвать с флагом гарантии доставки (не ждёт завершение выполнения return null)
        /// </summary>
        Guaranteed,
        /// <summary>
        /// Вызвать и дождаться ответа о завершение выполнения - иначе результат выполнения не будет получен
        /// </summary>
        ReturnWait,
    }
}
