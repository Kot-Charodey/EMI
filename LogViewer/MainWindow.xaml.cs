using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using EMI.Debug;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;

namespace LogViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum Mode
        {
            NetClient,
            OpenFile,
        }

        Mode ViewerMode;
        List<Message> Messages = new List<Message>();


        //=====NET
        TcpClient Client;
        NetworkStream NetworkStream;
        const int NetBufferSize = 5000 * 16;
        byte[] NetBuffer;

        public static MainWindow CreateNetClient(TcpClient tcp)
        {
            MainWindow window = new MainWindow();
            window.Client = tcp;
            window.NetworkStream = tcp.GetStream();
            window.ViewerMode = Mode.NetClient;

            Task.Run(window.MessageHandler);

            return window;
        }

        private MainWindow()
        {
            InitializeComponent();
        }

        private async void MessageHandler()
        {
            NetBuffer = new byte[NetBufferSize];
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await Task.Delay(1);

            MessageAdd(Message.CreateMSG($"Log Viewer Version: {Assembly.GetExecutingAssembly().GetName().Version}", Net.LogTag.Debugger, stopwatch));
            for (int i = 0; i < 10000; i++)
            {
                MessageAdd(Message.CreateMSG("Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст Очень длинный текст ", Net.LogTag.Debugger, stopwatch));
                //await Task.Delay(5);
            }
            MessageBox.Show("DONE");

            while (true)
            {
                await NetworkStream.ReadAsync(NetBuffer, 0, NetBufferSize);
                Message message = new Message(NetBuffer, stopwatch);
#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
                Task.Run(() => MessageAdd(message));
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен

            }
        }

        /// <summary>
        /// Добавляет сообщение в список, обновляет интерфейс если сообщение необходимо отрисовать
        /// </summary>
        /// <param name="message"></param>
        private void MessageAdd(Message message)
        {
            lock (Messages)
            {
                Messages.Add(message);

                Dispatcher.Invoke(() =>
                {
                    //Container.Items.Add(new MessageInfo(message));
                });
            }
        }
    }
}
