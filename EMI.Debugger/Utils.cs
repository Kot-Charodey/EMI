using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.Debugger
{
    internal static class Utils
    {

        private static readonly string[] ByteSize = { "", "KB", "MB", "GB", "TB" };
        public static string GetByteSize(long size)
        {
            if (size < 1024)
                return $"{size} B";
            double _size = size;
            int i;
            for (i = 0; i < ByteSize.Length; i++)
            {
                if (_size < 1024)
                    break;
                _size /= 1024;
            }
            return $"{Math.Round(_size * 10) / 10} {ByteSize[i]}";
        }

        public static string ToReadableString(this TimeSpan span)
        {
            if (span.TotalMilliseconds <= 1000)
            {
                return $"{span.TotalMilliseconds} ms";
            }
            else if (span.TotalSeconds <= 60)
            {
                return $"{span.TotalSeconds} s";
            }
            else if (span.TotalMinutes <= 60)
            {
                return $"{span.TotalMinutes} m";
            }
            else if (span.TotalHours <= 24)
            {
                return $"{span.TotalHours} h";
            }
            else
            {
                return $"{span.TotalDays} d";
            }
        }
    }
}