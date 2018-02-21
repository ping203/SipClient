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
            string login = txtLogin.Password;
            string password = txtPassword.Password;
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
                string login = txtLogin.Password;
                string password = txtPassword.Password;
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
            this.Close();
        }
    }
}
