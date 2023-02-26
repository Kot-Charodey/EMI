using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Shared;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace EMI.Debugger
{
    using Controls;

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ChromelessWindow // Window
    {
        public Config MyConfig = new();
        public MainWindow()
        {
            LoadConfig();
            SfSkinManager.ApplyStylesOnApplication = true;
            InitializeComponent();
            MenuThemeInit();
            SfSkinManager.SetTheme(this, new Theme(MyConfig.ThemeName));
        }

        private void LoadConfig()
        {
            if (File.Exists("Config.json"))
            {
                var cfgJson = File.ReadAllText("Config.json");
                var cfg = JsonSerializer.Deserialize<Config>(cfgJson);
                if (cfg != null)
                {
                    MyConfig = cfg;
                }
                else
                {
                    SaveConfig();
                }
            }
            else
            {
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            string cfg = JsonSerializer.Serialize(MyConfig, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText("Config.json", cfg);
        }

        private void MenuThemeInit()
        {
            var cfgJson = File.ReadAllText("ThemesList.json");
            var cfg = JsonSerializer.Deserialize<string[]>(cfgJson);
            foreach (var name in cfg!)
            {
                MenuItemAdv menu = new()
                {
                    Header = name,
                };
                menu.Click += MenuItemClick;
                MenuTheme.Items.Add(menu);
            }
        }

        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            string themeName = (string)((MenuItemAdv)sender).Header;
            MyConfig.ThemeName = themeName;
            SfSkinManager.SetTheme(this, new Theme(themeName));
            SaveConfig();
        }

        private void ConnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!Connection.IsCreateOne)
            {
                Connection connection = new();
                Content.Children.Add(connection);
            }
        }


        private void MenuStateNGCArray_Click(object sender, RoutedEventArgs e)
        {
            StateNGCArray state = new();
            Content.Children.Add(state);
        }


        private void MenuClientInfo_Click(object sender, RoutedEventArgs e)
        {
            StateClientInfo state = new();
            Content.Children.Add(state);
        }

        private void MenuServerInfo_Click(object sender, RoutedEventArgs e)
        {
            StateServerInfo state = new();
            Content.Children.Add(state);
        }

    }

    public class Config
    {
        public string ThemeName { get; set; } = "MaterialDark";
    }
}
