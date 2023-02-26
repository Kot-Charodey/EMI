using Syncfusion.Windows.Tools.Controls;
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

namespace EMI.Debugger.Controls
{
    /// <summary>
    /// Логика взаимодействия для Connection.xaml
    /// </summary>
    public partial class Connection : ContentControl
    {
        public static bool IsCreateOne = false;

        public Connection()
        {
            IsCreateOne = true;
            InitializeComponent();
        }

        private async void Client_Disconnected()
        {
            await Task.Delay(500);
            Dispatcher.Invoke(() =>
            {
                CButton.Content = "Подключить";
                CButton.IsEnabled = true;
            });
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            IsCreateOne = false;
            EMIDClient.Disconnect -= Client_Disconnected;
        }

        private CancellationTokenSource? Cancel;
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Cancel != null)
            {
                if (!Cancel.IsCancellationRequested)
                {
                    CButton.Content = "Отмена...";
                    CButton.IsEnabled = false;
                    Cancel.Cancel();
                    Cancel = null;
                }
            }
            else
            {
                if (EMIDClient.Client.IsConnect)
                {
                    EMIDClient.Client.Disconnect();
                    CButton.IsEnabled = false;
                }
                else
                {
                    CButton.Content = "Отменить подключение";
                    Cancel = new(5000);
                    var status = await EMIDClient.Client.Connect(Address.Text, Cancel.Token);
                    if (Cancel != null)
                    {
                        if (status == true)
                        {
                            CButton.IsEnabled = true;
                            CButton.Content = "Отключить";
                        }
                        else
                        {
                            MessageBox.Show("Не удалось подключиться!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            CButton.IsEnabled = true;
                            CButton.Content = "Подключить";
                        }
                        Cancel = null;
                    }
                    else
                    {
                        CButton.IsEnabled = true;
                        CButton.Content = "Подключить";
                    }
                }
            }
        }

        private void ContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            EMIDClient.Disconnect += Client_Disconnected;
        }
    }
}