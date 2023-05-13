using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMI
{
    /// <summary>
    /// Доп функции для работы с Task (многопоточностью)
    /// </summary>
    public static class TaskUtilities
    {
        /// <summary>
        /// Дожидается выполнение функции (не асинхронной блокируйщей операции) в другом потоке асинхроно с возможность отмены
        /// </summary>
        /// <param name="function">функция на выполнение</param>
        /// <param name="tokenSource">передаётся именно TokenSource так как задача себя таким образом разбудит когда закончит выполняться</param>
        /// <returns>вернёт true если задача завершена без прерываний</returns>
        public static async Task<bool> InvokeAsync(RPCfunc function, CancellationTokenSource tokenSource)
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
                throw exception;
            }

            return threadEndWork;
        }
    }
}