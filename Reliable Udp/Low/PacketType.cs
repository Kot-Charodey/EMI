using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reliable_Udp.Low
{
    /// <summary>
    /// Тип отсылаймого пакета
    /// </summary>
    internal enum PacketType : byte
    {
        /// <summary>
        /// отправляется пару раз если соединение будет закрыто по какой либо причине
        /// </summary>
        Close,

        //этапы подклчения

        /// <summary>
        /// client -> server: хочу подключиться
        /// </summary>
        СonnectionRequest,
        /// <summary>
        /// server -> client: отправляет тест с числом
        /// </summary>
        ConnectionTest,
        /// <summary>
        /// client -> server: отвечает этим числом
        /// </summary>
        ConnectionTestResult,
        /// <summary>
        /// server -> client: подтверждает подключение
        /// </summary>
        ConnectionCompleate,

        /// <summary>
        /// отправляет время
        /// </summary>
        Ping0,
        /// <summary>
        /// принимает время и вычисляет пинг
        /// </summary>
        Ping1,
        /// <summary>
        /// требует список потерявшихся пакетов (если мы их обнаружили быстрее чем )
        /// </summary>
        RequestPackage,
        /// <summary>
        /// список доставленных пакетов
        /// </summary>
        PackageDelivered,

        /// <summary>
        /// просто отправить
        /// </summary>
        SndSimple,
        /// <summary>
        /// отправить и точно доставить
        /// </summary>
        SndGuaranteed,
        /// <summary>
        /// отправить и точно доставить + сообщение будет отдано только когда прийдёт его порядок
        /// </summary>
        SndGuaranteedOrder,

        /// <summary>
        /// отправить и точно доставить (сегмент пакета)
        /// </summary>
        SndGuaranteedSegment,
        /// <summary>
        /// отправить и точно доставить + сообщение будет отдано только когда прийдёт его порядок (сегмент пакета)
        /// </summary>
        SndGuaranteedOrderSegment,
    }
}