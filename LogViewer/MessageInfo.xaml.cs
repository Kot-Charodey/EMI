using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LogViewer
{
    /// <summary>
    /// Логика взаимодействия для MessageInfo.xaml
    /// </summary>
    public partial class MessageInfo : UserControl
    {
        public readonly Message Message;
        public static ImageSource Error = ImageSourceFromBitmap(Properties.Resources.Error);

        public MessageInfo(Message message)
        {
            Message = message;
            InitializeComponent();

            MessageLabel.Text = $"[{Message.Tag}] {Message.Text}";
            TimeSpan t = message.TimeRecord;
            TimeLabel.Content = $"PC Time: {Message.TimePC} Record Time: {t.Hours} h. {t.Minutes:D2} m. {t.Seconds:D2} s. {t.Milliseconds:D3} ms.";

            Image.Source = Error;
        }

        //If you get 'dllimport unknown'-, then add 'using System.Runtime.InteropServices;'
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);

        private static ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
    }
}
