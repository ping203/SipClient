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
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = pswLogin.Password;
            string password = pswPassword.Password;
            if (String.IsNullOrEmpty(login) || String.IsNullOrEmpty(password))
            {
                MessageBox.Show("Логин или пароль не корректны");
                return;
            }
            Verification(login, password);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string login = pswLogin.Password;
                string password = pswPassword.Password;
                if (String.IsNullOrEmpty(login) || String.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Логин или пароль не корректны");
                    return;
                }
                Verification(login, password);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        // Валидация введеных данных
        private void Verification(string login, string password)
        {
            // check password and login

            // open MainWindow
            PhoneWindow phoneWindow = new PhoneWindow();
            phoneWindow.Login = login;
            phoneWindow.Password = password;
            phoneWindow.Show();
            this.Close();
        }

        private void btnCloseClick(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingForm = new SettingsWindow();
            settingForm.Owner = this;

            settingForm.txtHostAddress.Text = host;
            settingForm.txtPassword.Text = password;
            settingForm.txtLogin.Text = login;
            settingForm.XmlSettings = xml_settings;

            this.Visibility = System.Windows.Visibility.Hidden;
            if (settingForm.ShowDialog() == true)
            {
                this.Visibility = System.Windows.Visibility.Visible;
                LoadConfigure(Properties.Resources.SettingsFile);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfigure(Properties.Resources.SettingsFile);
        }

        private void LoadConfigure(string pathToFile)
        {
            try
            {
                xml_settings = new XmlDocument();
                xml_settings.Load(new FileStream(pathToFile, FileMode.Open, FileAccess.Read));
                var xRoot = xml_settings.DocumentElement;
                host = Convert.ToString(xRoot["Default"]["Host"].Attributes.GetNamedItem("ip").Value);
                login = Convert.ToString(xRoot["Default"]["Login"].InnerText);
                password = Convert.ToString(xRoot["Default"]["Password"].InnerText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            // auto input
            if (!String.IsNullOrEmpty(host) && !String.IsNullOrEmpty(login) && !String.IsNullOrEmpty(password))
            {
                pswLogin.Password = login;
                pswPassword.Password = password;
            }
        }

        private string host = String.Empty;
        private string login = String.Empty;
        private string password = String.Empty;

        private static XmlDocument xml_settings;
    }
}
