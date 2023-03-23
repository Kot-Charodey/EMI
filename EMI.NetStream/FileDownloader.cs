using EMI.Indicators;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EMI.NetStream
{
    /// <summary>
    /// Клиент для закрузки файлов с хоста
    /// </summary>
    public class FileDownloader
    {
        /// <summary>
        /// Информация о прогрессе загрузки
        /// </summary>
        public struct Info
        {
            public long TotalLength;
            public long DownloadedLength;
            public double Progress;

            public override string ToString()
            {
                return $"TotalLength: {TotalLength/1024} KB\nDownloadedLength: {DownloadedLength/1024} KB\nProgress: {Math.Round(Progress,3)}%";
            }
        }
        
        /// <summary>
        /// Скачать файл по сети
        /// </summary>
        /// <param name="client">EMI клиент хост файл</param>
        /// <param name="ID">айди хоста</param>
        /// <param name="fileName">имя файла на хосте</param>
        /// <param name="stream">поток для записи файла</param>
        /// <param name="progress">информация о загрузке файла (другой поток)</param>
        /// <param name="bufferSize">размер буфера</param>
        /// <returns>если false файла на сервере не существует</returns>
        public static async Task<bool> Download(Client client, int ID, string fileName, Stream stream,Action<Info> progress,int bufferSize = 4096)
        {
            var fileHost = new Indicator.FuncOut<(bool, int), string>("FileHost_GetFileIndicator_" + ID);
            var result = await fileHost.RCall(fileName, client).ConfigureAwait(false);

            if (result.Item1 == false)
                return false;

            var remote = await NetStreamRemote.Open(client, result.Item2).ConfigureAwait(false);
            Exception err = null;
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    byte[] buffer = new byte[bufferSize];
                    Info info = new Info
                    {
                        TotalLength = remote.Length
                    };

                    long pos = remote.Position;

                    if (pos != 0)
                    {
                        remote.Position = 0;
                        if (remote.Position != 0)
                        {
                            throw new NotSupportedException();
                        }
                        else
                        {
                            pos = 0;
                        }
                    }

                    while (pos < remote.Length)
                    {
                        int readSize = remote.Read(buffer, 0, (int)Math.Min(bufferSize, info.TotalLength - pos));
                        stream.Write(buffer, 0, readSize);

                        pos = remote.Position;

                        info.Progress = (double)remote.Position / info.TotalLength * 100;
                        info.DownloadedLength = pos;

                        progress.Invoke(info);
                    }
                }
                catch(Exception e)
                {
                    err = e;
                }
                finally
                {
                    try
                    {
                        remote.Close();
                    }
                    catch { }
                }
            }, TaskCreationOptions.LongRunning);

            if (err != null)
                throw err;

            return true;
        }
    }
}
