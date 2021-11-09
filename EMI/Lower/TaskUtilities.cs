using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EMI.Lower
{
    internal static class TaskUtilities
    {
        /// <summary>
        /// Позволяет выполнить блокирующию операцию с функцией отмены и асинхронно (в другом потоке)
        /// </summary>
        /// <param name="function">функция на выполнение</param>
        /// <param name="tokenSource">передаётся именно TokenSource так как задача себя таким образом разбудит когда закончит выполняться</param>
        /// <returns>вернёт true если задача завершена без прерываний</returns>
        public static async Task<bool> InvokeAsync(RPCfunct function, CancellationTokenSource tokenSource)
        {
            Exception exception = null;
            bool threadEndWork = false;

            Thread th = new Thread(() =>
                {
                    try
                    {
                        function.Invoke();
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                    threadEndWork = true;
                    tokenSource.Cancel();
                })
            {
                IsBackground = true
            };
            th.Start();
            try
            {
                await Task.Delay(-1, tokenSource.Token);
            }
            catch { }

            if (!threadEndWork)
            {
                try
                {
                    th.Abort();
                }
                catch
                {

                }
            }

            if (exception != null)
            {
                throw new Exception();
            }

            return threadEndWork;
        }
    }
}