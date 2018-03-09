using System;
using System.Collections.Generic;
using System.IO;
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

            this.Activated += new EventHandler(Login_Activated);
        }

        void Login_Activated(object sender, EventArgs e)
        {
            if (ignoreFlag)
            {
                Verification(login, password);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string path = System.IO.Path.GetFullPath(Properties.Resources.PathToSettings);

            if (!System.IO.File.Exists(path))
                path = @"./Settings.xml";
            LoadConfigure(path);
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

            // Create PhoneWindow
            PhoneWindow phoneWindow = new PhoneWindow();
            phoneWindow.Login = login;
            phoneWindow.Password = password;
            phoneWindow.Host = host;
            phoneWindow.Show();
            // Close Login Window
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

            this.Visibility = System.Windows.Visibility.Hidden;

            if (settingForm.ShowDialog() == true)
            {
                this.Visibility = System.Windows.Visibility.Visible;
                LoadConfigure(Properties.Resources.PathToSettings);
            }
        }

        private void LoadConfigure(string pathToFile)
        {
            try
            {
                XmlDocument xml_settings = new XmlDocument();
                using (FileStream fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
                {
                    xml_settings.Load(fs);

                    var xRoot = xml_settings.DocumentElement;
                    host = Convert.ToString(xRoot["Default"]["Host"].Attributes.GetNamedItem("Ip").Value);
                    login = Convert.ToString(xRoot["Default"]["Login"].InnerText);
                    password = Convert.ToString(xRoot["Default"]["Password"].InnerText);
                    ignoreFlag = Convert.ToBoolean(xRoot["Default"].Attributes["Ignore"].Value);

                    MSQ.MessageQueueFactory.PathToOrdersMQ = Convert.ToString(xRoot["Default"]["MessageQueue"]["Orders"].Attributes["Path"].Value);

                    MSQ.MessageQueueFactory.PathToSipClientMQ = Convert.ToString(xRoot["Default"]["MessageQueue"]["SipClient"].Attributes["Path"].Value);
                }
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
        private bool ignoreFlag = false;
    }
}
