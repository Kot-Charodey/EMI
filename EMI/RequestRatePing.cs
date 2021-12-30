#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
using System;

namespace EMI
{
    /// <summary>
    /// Содержит функции для работы с ping
    /// </summary>
    public static class Ping
    {
        /// <summary>
        /// Массив задержек для GetRequestRateDelay
        /// </summary>
        internal static readonly TimeSpan[] RequestRateDelay = {
            new TimeSpan(0,0,0,0,5),
            new TimeSpan(0,0,0,0,10),
            new TimeSpan(0,0,0,0,100),
            new TimeSpan(0,0,0,0,250),
            new TimeSpan(0,0,0,0,500),
            new TimeSpan(0,0,0,0,1000),
            new TimeSpan(0,0,0,0,2000),
            new TimeSpan(0,0,0,0,5000),
            new TimeSpan(0,0,0,0,10000),
            new TimeSpan(0,0,0,0,30000),
            new TimeSpan(0,0,0,0,60000),
            new TimeSpan(0,0,0,0,120000),
        };

        internal static TimeSpan GetRequestRateDelay(RequestRate rate)
        {
            return RequestRateDelay[(int)rate];
        }

        /// <summary>
        /// Частота опроса Ping
        /// </summary>
        public enum RequestRate
        {
            ms5,
            ms10,
            ms100,
            ms250,
            ms500,
            ms1000,
            ms2000,
            ms5000,
            ms10000,
            ms30000,
            ms120000,
        }

        /// <summary>
        /// Тип опроса
        /// </summary>
        public enum RequestType
        {
            /// <summary>
            /// Опрос согласно частоте RequestRate
            /// </summary>
            Timer,
            /// <summary>
            /// Запрос пинг монтируется в каждый пакет (при вызове функции можно так же указать о данной необходимости для некоторых пакетов не используя RequestType.InPacketPing) и возможно точно узнать задержу при обработке данных пакета (используйте спец функцию что бы узнать ping пакета)
            /// </summary>
            InPacketPing,
            /// <summary>
            /// Включает оба варианта
            /// </summary>
            All,
        }
    } 
    
}

#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена