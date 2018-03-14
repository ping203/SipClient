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
            string path = System.IO.Path.GetFullPath(Properties.Resources.PathToSettings);

            if (!System.IO.File.Exists(path))
                path = @"./Settings.xml";

            string host = txtHostAddress.Text;
            string login = txtLogin.Text;
            string password = txtPassword.Text;
            
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

                xDoc.Save(System.IO.Path.GetFullPath(Properties.Resources.PathToSettings));
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
            this.DialogResult = true;
            this.Close();
        }
    }
}
