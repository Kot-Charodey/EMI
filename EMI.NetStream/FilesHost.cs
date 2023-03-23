using EMI.Indicators;
using System;
using System.IO;

namespace EMI.NetStream
{
    /// <summary>
    /// Создаёт хост для раздачи файлов
    /// </summary>
    public class FilesHost
    {
        private Client Client;
        private Func<string, Stream> Func;
        private Indicator.FuncOut<(bool, int), string> GetFileIndicator;

        /// <summary>
        /// Создать хост для раздачи файлов
        /// </summary>
        /// <param name="client">EMI клиент</param>
        /// <param name="ID">айди хоста</param>
        /// <param name="getFile">запрос на поток для считывания файла</param>
        public FilesHost(Client client, int ID, Func<string, Stream> getFile)
        {
            Client = client;
            Func = getFile;

            GetFileIndicator = new Indicator.FuncOut<(bool, int), string>("FileHost_GetFileIndicator_" + ID);
            Client.LocalRPC.RegisterMethod(GetFile, GetFileIndicator);
        }

        private (bool, int) GetFile(string filePath)
        {
            var data = Func(filePath);

            if (data == null)
                return (false, -1);

            var hostID = NetStreamHost.Create(Client, data);

            return (true, hostID);
        }


    }
}
