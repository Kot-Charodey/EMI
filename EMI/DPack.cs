using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartPackager;

namespace EMI
{
    internal static class DPack
    {
        public static Packager.M<DateTime> DPing = Packager.Create<DateTime>();
        public const int sizeof_DPing = 4;

        public static Packager.M<int> DRPC = Packager.Create<int>();
        public const int sizeof_DRPC = 4;

        public static Packager.M<bool,int> DForwarding = Packager.Create<bool,int>();
        public const int sizeof_DForwarding = 5;
    }
}
