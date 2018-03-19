using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.IO;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for CallStat.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static string password = String.Empty;
        private static string login = String.Empty;
        private static string host = String.Empty;
        public static string PathToConfigs = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Properties.Resources.SettingsFileName);

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings(PathToConfigs);

            this.txtHostAddress.Text = host;
            this.txtLogin.Text = login;
            this.txtPassword.Password = password;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnAppend_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings(PathToConfigs);
            this.DialogResult = true;
            this.Close();
        }

        public void SaveSettings(string path)
        {
            string host = txtHostAddress.Text;
            string login = txtLogin.Text;
            string password = txtPassword.Password;

            if (host == null && login == null && password == null)
                return;

            try
            {
                XmlDocument xDoc = new XmlDocument();
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    xDoc.Load(fs);
                }

                xDoc.DocumentElement["Default"]["Host"].Attributes.GetNamedItem("Ip").Value = host;

                xDoc.DocumentElement["Default"]["Login"].InnerText = login;

                xDoc.DocumentElement["Default"]["Password"].InnerText = password;

                xDoc.Save(SettingsWindow.PathToConfigs);
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
                    host = Convert.ToString(xRoot["Default"]["Host"].Attributes.GetNamedItem("Ip").Value);
                    login = Convert.ToString(xRoot["Default"]["Login"].InnerText);
                    password = Convert.ToString(xRoot["Default"]["Password"].InnerText);

                    PhoneWindow.Host = host;
                    PhoneWindow.Login = login;
                    PhoneWindow.Password = password;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
