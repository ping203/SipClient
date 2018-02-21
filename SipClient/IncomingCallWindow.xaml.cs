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
using System.Media;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for IncomingCallWindow.xaml
    /// </summary>
    public partial class IncomingCallWindow : Window
    {
        private static SoundPlayer soundPlayer = new SoundPlayer();

        public Ozeki.VoIP.DialInfo RingingUser { get; set; }

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Play Sound
            soundPlayer.Stream = Properties.Resources.signal;
            soundPlayer.PlayLooping();  

            // Load Inforamtion
            this.txtCallName.Text = RingingUser.CallerID;
            this.txtPhoneNumber.Text = RingingUser.DialedString;
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
            
        }

        private void btnHoldOn_Click(object sender, RoutedEventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
        }

        public Ozeki.VoIP.IPhoneCall Call { get; set; }
    }
}
