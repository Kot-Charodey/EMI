using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
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
    using EMI.DebugServer;
    /// <summary>
    /// Логика взаимодействия для StateNGCArray.xaml
    /// </summary>
    public partial class StateClientInfo : ContentControl
    {
        public StateClientInfo()
        {
            InitializeComponent();
            ClientList.SelectionChanged += ClientList_SelectionChanged;
        }

        private void ClientList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LogsList.Items.Clear();
        }

        private long LastRPCHEX = 0;

        private void DataRender()
        {
            Dispatcher.Invoke(() =>
            {
                if (EMIDClient.Data != null)
                {
                    int sindex = ClientList.SelectedIndex;
                    if (ClientList.Items.Count != EMIDClient.Data.ClientsInfo.Count)
                    {
                        ClientList.Items.Clear();
                        foreach (var cc in EMIDClient.Data.ClientsInfo.Values)
                        {
                            TextBlock block = new()
                            {
                                Text = cc.ClientInfo.Address,
                                DataContext = cc
                            };
                            if (cc.ClientInfo.ClientID == default)
                            {
                                block.Text = "Server";
                            }

                            ClientList.Items.Add(block);
                        }
                        ClientList.SelectedIndex = sindex;
                    }
                    if (sindex > -1)
                    {
                        var client = EMIDClient.Data.ClientsInfo.Values.ElementAt(sindex);

                        switch (TabClientInfo.SelectedIndex)
                        {
                            case 0:
                                IsConnect.Text = client.ClientInfo.IsConnect + "";
                                IsServerSide.Text = client.ClientInfo.IsServerSize + "";
                                Ping.Text = client.ClientInfo.Ping.ToReadableString();
                                PingTimeout.Text = client.ClientInfo.PingTimeout.ToReadableString();
                                MaxPacketAcceptSize.Text = Utils.GetByteSize(client.ClientInfo.MaxPacketAcceptSize);
                                break;
                            case 1:
                                long hex = 0;
                                foreach (var rpc in client.RPC.RegisteredMethods)
                                    hex += rpc.ID;
                                foreach (var rpc in client.RPC.RegisteredForwarding)
                                    hex += rpc.ID;
                                if (hex == LastRPCHEX)
                                    break;
                                LastRPCHEX = hex;

                                RPCList.Items.Clear();

                                foreach(var rpc in client.RPC.RegisteredMethods)
                                {
                                    TextBlock panel = new()
                                    {
                                        Text = $"RPC   Имя: {rpc.Name}    ИД: {rpc.ID:X}    Зарегистрировано: {rpc.Count}"
                                    };

                                    RPCList.Items.Add(panel);
                                    RPCList.Items.Add(new Separator());
                                }


                                foreach (var rpc in client.RPC.RegisteredForwarding)
                                {
                                    TextBlock panel = new()
                                    {
                                        Margin = new Thickness(0, 0, 0, 5),
                                        Text = $"RPC Forwarding   Имя: {rpc.Name}    ИД: {rpc.ID:X}"
                                    };

                                    RPCList.Items.Add(panel);
                                    RPCList.Items.Add(new Separator());
                                }
                                break;
                            case 2:
                                if (LogsList.Items.Count != client.Logs.Count)
                                {
                                    LogsList.Items.Clear();
                                    foreach (var msg in client.Logs)
                                    {
                                        TextBlock panel = new()
                                        {
                                            Text = $"{msg.Time}    IsServerMSG {msg.IsServerMSG}    Type {msg.Type}\nMessage:\n{msg.Message}"
                                        };
                                        StackPanel sp = new();
                                        sp.Children.Add(panel);
                                        sp.Children.Add(new Separator());

                                        LogsList.Items.Add(sp);
                                    }
                                }
                                break;
                        }
                        
                    }
                }
            });
        }

        private void ContentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            EMIDClient.OnDataGet -= DataRender;
        }

        private void ContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            EMIDClient.OnDataGet += DataRender;
        }
    }
}
