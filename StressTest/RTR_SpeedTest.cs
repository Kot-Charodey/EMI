using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Net;
using EMI;

namespace StressTest
{
    public class RTR_SpeedTest:TestBase
    {
        RPCAddressOut<int, int[]> SumR;

        protected override void Init(RPCAddressTable table)
        {
            SumR = new RPCAddressOut<int, int[]>(table);

            RPC.Global.RegisterMethod(SumR, 0, Sum);
        }

        protected override void ServerNewUser(Client client)
        {
            
        }

        protected override void ClientProcess(Client client)
        {
            client.RequestRatePing = RequestRatePing.ms10;

            Stopwatch stopwatch = new Stopwatch();
            long workCounter = 0;

            bool error = false;
            stopwatch.Start();

            Thread thread = new Thread(() =>
            {
                int[] arr = new int[5000];
                int s = 0;
                for (int i = 0; i < arr.Length; i++)
                {
                    s += arr[i] = i;
                }

                while (true)
                {
                    int num = client.RemoteGuaranteedExecution(SumR, arr).Result;

                    if (num == s)
                    {
                        workCounter++;
                    }
                    else
                    {
                        error = true;
                    }
                }

            });
            thread.Start();

            while (true)
            {
                StandLogClient.Type1(client,workCounter,error,stopwatch);
            }
        }

        int Sum(int[] data)
        {
            int s = 0;
            for (int i = 0; i < data.Length; i++)
            {
                s += data[i];
            }
            return s;
        }
    }
}
