using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI
{
    /// <summary>
    /// Метод вызываемый когда нужно переслать сообщения (возвращает указать кому требуется переслать сообщение)
    /// </summary>
    /// <param name="owner">Владелец сообщения</param>
    /// <returns></returns>
    public delegate Client[] ForwardingMethod(Client owner);
}
