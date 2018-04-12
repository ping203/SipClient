using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace SipClient.View
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : MetroWindow
    {
        public static bool isEchoOff { get; private set; }
        public static string PathToConfigs =
            string.Concat(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Properties.Resources.SettingsFileName);

        public static string Account { get; private set; }
        public static int Port { get; private set; }
        public static string Password { get; private set; }
        public static string Login { get; private set; }
        public static string Host { get; private set; }

        private Func<string, bool> isMicrophone = (device) => Regex.Match(device, "(Микрофон)").Success;
        private Func<string, bool> isSpeaker = (device) => Regex.Match(device, "(Динамики)").Success;

        public Settings()
        {
            InitializeComponent();
        }

        private void btnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnAppend_Click(object sender, RoutedEventArgs e)
        {
            //echo cancellation
            bool? echo = this.tswEchoCancellation.IsChecked;
            if (echo != null)
                isEchoOff = (bool)echo;

            //set record-playback devices
            if (choosedPlaybackDevice != null)
                MainWindow.Softphone.PlaybackDevice = choosedPlaybackDevice;
            if (choosedCaptureDevice != null)
                MainWindow.Softphone.CaptureDevice = choosedCaptureDevice;

            SaveSettings(PathToConfigs);
            this.DialogResult = true;
            this.Close();
        }

        public void SaveSettings(string path)
        {
            string host = txtHostAddress.Text;
            string login = txtLogin.Text;
            string password = txtPassword.Password;
            string port = txtPort.Text;
            Account = (txtAccountName.Text == "") ? login : txtAccountName.Text;

            try
            {
                XmlDocument xDoc = new XmlDocument();
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    xDoc.Load(fs);
                }

                xDoc.DocumentElement["Connection"].Attributes.GetNamedItem("Account").Value = Account;
                xDoc.DocumentElement["Connection"].Attributes.GetNamedItem("Ip").Value = host;
                xDoc.DocumentElement["Connection"].Attributes.GetNamedItem("Port").Value = port;
                xDoc.DocumentElement["Connection"].Attributes.GetNamedItem("Login").Value = login;
                xDoc.DocumentElement["Connection"].Attributes.GetNamedItem("Password").Value = password;

                xDoc.DocumentElement["EchoCancellation"].Attributes.GetNamedItem("Value").Value = isEchoOff.ToString();
                // save document
                xDoc.Save(PathToConfigs);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        public static void LoadSettings(string path)
        {
            try
            {
                XmlDocument xml_settings = new XmlDocument();
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    xml_settings.Load(fs);

                    var xRoot = xml_settings.DocumentElement;
                    Account = Convert.ToString(xRoot["Connection"].Attributes.GetNamedItem("Account").Value);
                    Host = Convert.ToString(xRoot["Connection"].Attributes.GetNamedItem("Ip").Value);
                    Port = Convert.ToInt32(xRoot["Connection"].Attributes.GetNamedItem("Port").Value);
                    Login = Convert.ToString(xRoot["Connection"].Attributes.GetNamedItem("Login").Value);
                    Password = Convert.ToString(xRoot["Connection"].Attributes.GetNamedItem("Password").Value);

                    isEchoOff = Convert.ToBoolean(xRoot["EchoCancellation"].Attributes.GetNamedItem("Value").Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnMinimizeClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load xml settings
            LoadSettings(PathToConfigs);

            this.txtHostAddress.Text = Host;
            this.txtLogin.Text = Login;
            this.txtPassword.Password = Password;
            this.txtPort.Text = Port.ToString();
            this.txtAccountName.Text = Account;

            this.tswEchoCancellation.IsChecked = isEchoOff;

            // Load device list
            List<string> playbackDevices = new List<string>();
            playbackDevices.Add("Устройство воспроизведения по умолчанию");
            List<string> recordDevices = new List<string>();
            recordDevices.Add("Устройство записи по умолчанию");

            spltPlaybackDevices.SelectedIndex = 0;
            spltRecordDevices.SelectedIndex = 0;

            if (MainWindow.Softphone.CurrentConnectState == sipdotnet.Phone.ConnectState.Connected)
            {
                playbackDevices.AddRange(MainWindow.Softphone?.PlaybackDevices());
                recordDevices.AddRange(MainWindow.Softphone?.CaptureDevices());

                spltPlaybackDevices.ItemsSource = playbackDevices;
                spltRecordDevices.ItemsSource = recordDevices;

                spltPlaybackDevices.SelectedItem = MainWindow.Softphone.PlaybackDevice;
                spltRecordDevices.SelectedItem = MainWindow.Softphone.CaptureDevice;

                spltPlaybackDevices.SelectionChanged += new SelectionChangedEventHandler(spltDevices_SelectionChanged);
                spltRecordDevices.SelectionChanged += new SelectionChangedEventHandler(spltDevices_SelectionChanged);

                // echo cancellation                
            }

            // инфа
            infoPanel.Text += $"PlaybackDevices List : {Environment.NewLine}{ string.Join(Environment.NewLine, MainWindow.Softphone.PlaybackDevices()) }{Environment.NewLine}{new string('-', 40)}{Environment.NewLine}";
            infoPanel.Text += $"CaptureDevices List : {Environment.NewLine}{ string.Join(Environment.NewLine, MainWindow.Softphone.CaptureDevices()) }{Environment.NewLine}{new string('-', 40)}{Environment.NewLine}";
            infoPanel.Text += $"PlaybackDevice : { MainWindow.Softphone.PlaybackDevice }{Environment.NewLine}";
            infoPanel.Text += $"CaptureDevice : { MainWindow.Softphone.CaptureDevice }{Environment.NewLine}";
            infoPanel.Text += $"RingerDevice : { MainWindow.Softphone.RingerDevice }{Environment.NewLine}";
            infoPanel.Text += $"Микрофон доступен : { MainWindow.Softphone.MicrophoneEnabled }{Environment.NewLine}";
        }

        private string choosedPlaybackDevice;
        private string choosedCaptureDevice;

        void spltDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var splitButton = (sender as SplitButton);
            var choosedItem = e.AddedItems[0].ToString();

            if (choosedItem != null)
            {
                if (splitButton?.Name == "spltPlaybackDevices")
                {
                    choosedPlaybackDevice = choosedItem;
                }
                if (splitButton?.Name == "spltRecordDevices")
                {
                    choosedCaptureDevice = choosedItem;
                }
            }
        }
    }
}
