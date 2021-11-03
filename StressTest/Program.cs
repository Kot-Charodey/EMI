using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StressTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //new RTR_SpeedTest().Run();
            new Threads_Test().Run();
        }
    }
}