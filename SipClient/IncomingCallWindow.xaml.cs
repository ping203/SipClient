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

        public sipdotnet.Call Call { get; set; }

        public sipdotnet.Phone SoftPhone { get; set; }

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

        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Play Sound
            soundPlayer.Stream = Properties.Resources.signal;
            soundPlayer.PlayLooping();
#warning ex1
            SoftPhone.GetMediaHandler.EchoCancellation(Call, true);
            // Load Information
            
            SetAttributes(Phone, Name, Address);
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();

            // принимаем звонок 
            SoftPhone.ReceiveOrResumeCall(this.Call);
        }

        private void btnHoldOn_Click(object sender, RoutedEventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
            // Если удерживаем звонок
            if (Call != null)
            {
                SoftPhone.HoldCall(this.Call);
            }
        }

        private void btnReject_Click(object sender, RoutedEventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
            // отклоняем звонок 
            if (Call != null)
            {
                SoftPhone.TerminateCall(this.Call);
            }
            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
            if (this.Call != null && this.Call.GetState() != sipdotnet.Call.CallState.None)
                SoftPhone.TerminateCall(this.Call);
        }

        internal void SetAttributes(string phone, string name, string address)
        {
            this.txtNameAndPhone.Text = string.Format("Имя : {0}   Телефон : {1}", name, phone);
            this.txtAddress.Text = address;
        }
    }
}
