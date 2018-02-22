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

namespace SipClient
{
    /// <summary>
    /// Interaction logic for CallStat.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnAppend_Click(object sender, RoutedEventArgs e)
        {
            string host = txtHostAddress.Text;
            string login = txtLogin.Text;
            string password = txtPassword.Text;
            
            if (host == null && login == null && password == null)
                return;

            try
            {
                XmlWriter xmlWriter = XmlWriter.Create(Properties.Resources.SettingsFile);

                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("Settings");
                xmlWriter.WriteStartElement("Default");

                xmlWriter.WriteStartElement("Host");
                xmlWriter.WriteAttributeString("ip", host);
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Login");
                xmlWriter.WriteString(login);
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("Password");
                xmlWriter.WriteString(password);
                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndDocument();
                xmlWriter.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
            this.DialogResult = true;
            this.Close();
        }

        public System.Xml.XmlDocument XmlSettings { get; set; }
    }
}
