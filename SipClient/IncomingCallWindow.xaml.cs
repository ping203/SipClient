using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
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
    /// Interaction logic for IncomingCallWindow.xaml
    /// </summary>
    public partial class IncomingCallWindow : Window
    {
        private static SoundPlayer soundPlayer = new SoundPlayer();

        public IncomingCallWindow()
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

        public string Name = String.Empty;
        public string Phone = String.Empty;
        public string Address = String.Empty;

        public Ozeki.VoIP.IPhoneCall Call { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Play Sound
            soundPlayer.Stream = Properties.Resources.signal;
            soundPlayer.PlayLooping();
            // Load Inforamtion
            SetAttributes(Phone, Name, Address);
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();

            if (Call.CallState == Ozeki.VoIP.CallState.LocalHeld)
            {
                Call.ToggleHold();
            }
            else
            {
                // принимаем звонок 
                Call.Answer();
            }
        }

        private void btnHoldOn_Click(object sender, RoutedEventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
            // Если удерживаем звонок
            if (Call.CallState != Ozeki.VoIP.CallState.LocalHeld)
            {
                Call.ToggleHold();
            }
        }

        private void btnTransferTo_Click(object sender, RoutedEventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
            if (Call != null && Call.IsAnswered)
            {               
                this.Hide();
                
            }
        }

        private void btnReject_Click(object sender, RoutedEventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
            // отклоняем звонок 
            if (Call.IsAnswered)
            {
                Call.HangUp();
            }
            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
            if (Call != null && Call.IsAnswered)
                Call.HangUp();
        }

        internal void SetAttributes(string phone, string name, string address)
        {
            this.txtNameAndPhone.Text = string.Format("Имя : {0}   Телефон : {1}", name, phone);
            this.txtAddress.Text = address;
        }
    }
}
