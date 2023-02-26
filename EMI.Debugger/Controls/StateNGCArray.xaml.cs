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
    public partial class StateNGCArray : ContentControl
    {
        public StateNGCArray()
        {
            InitializeComponent();
        }

        private void DataRender()
        {
            Dispatcher.Invoke(() =>
            {
                if (EMIDClient.Data != null)
                {
                    try
                    {
                        var info = EMIDClient.Data.NGC.Last();
                        {
                            long all = (long)info.FreeArraysCount + info.UseArrays;
                            double progress = (double)info.UseArrays / (all) * 100;
                            ArrayProgress.Value = progress;
                            ArrayText.Text = $"{info.UseArrays} из {all} [{Math.Round(progress * 10) / 10}%]";
                        }

                        {
                            long all = info.TotalFreeArraysSize + info.TotalUseSize;
                            double progress = (double)info.TotalUseSize / (all) * 100;
                            MemoryProgress.Value = progress;
                            MemoryText.Text = $"{Utils.GetByteSize(info.TotalUseSize)} из {Utils.GetByteSize(all)} [{Math.Round(progress * 10) / 10}%]";
                        }
                    }
                    catch (Exception g)
                    {
                        System.Diagnostics.Debug.WriteLine(g.ToString());
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
