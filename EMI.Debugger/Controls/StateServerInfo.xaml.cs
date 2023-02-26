using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EMI.Debugger.Controls
{
    /// <summary>
    /// Логика взаимодействия для StateServerInfo.xaml
    /// </summary>
    public partial class StateServerInfo : ContentControl
    {
        public StateServerInfo()
        {
            InitializeComponent();
        }

        private void DataRender()
        {
            Dispatcher.Invoke(() =>
            {
                if (EMIDClient.Data != null)
                {
                    var data = EMIDClient.Data;
                    InfoServerAddress.Text = data.ServerInfo.Address;
                    int cc = 0;
                    foreach (var client in data.ClientsInfo.Values)
                        if (client.ClientInfo.IsConnect)
                            cc++;

                    InfoServerClientsCount.Text = cc.ToString();
                    InfoServerIsRun.Text = data.ServerInfo.IsRun.ToString();
                    InfoServiceName.Text = data.ServerInfo.ServiceName;
                }
            });
        }

        private void ContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            EMIDClient.OnDataGet += DataRender;
        }

        private void ContentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            EMIDClient.OnDataGet -= DataRender;
        }
    }
}
