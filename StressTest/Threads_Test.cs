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
    public class Threads_Test: TestBase
    {
        RPCAddressOut<int, data> SumR;

        struct data
        {
            public int a;
            public int b;
        }

        protected override void Init(RPCAddressTable table)
        {
            SumR = new RPCAddressOut<int, data>(table);

            RPC.Global.RegisterMethod(SumR, 0, Sum);
        }

        protected override void ServerNewUser(Client client)
        {
            
        }

        protected override void ClientProcess(Client client)
        {
            client.RequestRatePing = EMI.Ping.RequestRate.ms10;

            Stopwatch stopwatch = new Stopwatch();
            long workCounter = 0;

            bool error = false;
            stopwatch.Start();

            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    Thread[] ths = new Thread[1];
                    for (int i = 0; i < ths.Length; i++)
                    {
                        ths[i] = new Thread(() =>
                        {
                            data data = new data()
                            {
                                a = 400000000,
                                b = 1,
                            };

                            int res = client.RemoteGuaranteedExecution(SumR, data).Result;
                            lock (stopwatch)
                            {
                                if (res == (data.a + data.b))
                                {
                                    workCounter++;
                                }
                                else
                                {
                                    error = true;
                                }
                            }
                        });
                    }

                    foreach(var t in ths)
                    {
                        t.Start();
                    }

                    foreach (var t in ths)
                    {
                        t.Join();
                    }
                }

            });
            thread.Start();

            while (true)
            {
                StandLogClient.Type1(client, workCounter, error, stopwatch);
            }
        }

        int Sum(data data)
        {
            return data.a + data.b;
        }
    }
}
