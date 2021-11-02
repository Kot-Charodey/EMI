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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net;
using System.Net.Sockets;

namespace LogViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class Connect : Window
    {
        public Connect()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ushort port;
            if (!ushort.TryParse(PortText.Text, out port))
            {
                MessageBox.Show($"'{PortText.Text}' не является допустимым числом сетевого порта!");
                return;
            }
            IPAddress address = null;
            string ip = IPText.Text;
            if (ip.ToLower() == "localhost")
            {
                address = IPAddress.Loopback;
            }
            else if (!IPAddress.TryParse(IPText.Text, out address))
            {
                MessageBox.Show($"'{IPText.Text}' не является допустимым ip адрессом!");
                return;
            }
            IPEndPoint point = new IPEndPoint(address, port);
            TcpClient client = new TcpClient();
            Wait wait = new Wait();
            Thread th = new Thread(() =>
              {
                  try
                  {
                      client.Connect(point);
                  }
                  catch {
                      MessageBox.Show("Не удалось подключиться");
                  }
                  wait.Dispatcher.Invoke(() => wait.Close());
              });
            th.IsBackground = true;
            th.Start();
            wait.ShowDialog();
            try
            {
                if (th.IsAlive)
                {
                    th.Abort();
                }
            }
            catch { }

            if (client.Connected)
            {
                MainWindow mw = MainWindow.CreateNetClient(client);
                mw.Show();
                this.Close();
            }
            else
            {

            }
        }
    }
}