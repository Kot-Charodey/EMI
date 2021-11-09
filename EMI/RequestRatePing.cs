#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
using System;

namespace EMI
{
    public static class Ping
    {
        internal static readonly TimeSpan[] RequestRateDelay = {
            new TimeSpan(0,0,0,0,5),
            new TimeSpan(0,0,0,0,10),
            new TimeSpan(0,0,0,0,100),
            new TimeSpan(0,0,0,0,250),
            new TimeSpan(0,0,0,0,500),
            new TimeSpan(0,0,0,0,1000),
            new TimeSpan(0,0,0,0,2000)
        };

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
        }
    } 
    
}

#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена