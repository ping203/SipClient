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
using MahApps.Metro.Controls;
using System.Windows.Media.Animation;
using System.Timers;

namespace SipClient.View
{
    /// <summary>
    /// Interaction logic for IncomingDialog.xaml
    /// </summary>
    public partial class IncomingDialog : MetroWindow
    {
        public sipdotnet.Phone SoftPhone { get; set; }

        public sipdotnet.Call Call { get; set; }

        public IncomingDialog()
        {
            InitializeComponent();

            this.Opacity = 0;
            // set WindowStartupLocation
            this.Left = SystemParameters.PrimaryScreenWidth - 350;
            this.Top = SystemParameters.PrimaryScreenHeight - 230;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var animationEnd = new DoubleAnimation();
            animationEnd.From = this.Opacity;
            animationEnd.To = 0d;
            animationEnd.Duration = new Duration(TimeSpan.FromMilliseconds(600));
            BeginAnimation(OpacityProperty, animationEnd);
            using (var timer = new Timer())
            {
                timer.Interval = 600;
                timer.Enabled = true;
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var animationBegin = new DoubleAnimation();
            animationBegin.From = this.Opacity;
            animationBegin.To = 1d;
            animationBegin.Duration = new Duration(TimeSpan.FromMilliseconds(600));
            BeginAnimation(OpacityProperty, animationBegin);
        }

        private void btnAppend(object sender, RoutedEventArgs e)
        {
            if (this.Call != null)
            {
                SoftPhone.ReceiveOrResumeCall(this.Call);
            }
            this.Close();
        }

        private void btnClose(object sender, RoutedEventArgs e)
        {
            if (this.Call != null)
            {
                SoftPhone.TerminateCall(this.Call);
            }
            this.Close();
        }

        public void UpdateTextPanel(string phone, string name)
        {
            if (name != null)
                lblName.Content = "Имя : " + name;
            if (phone != null)
                lblPhone.Content = "Телефон : " + phone;
        }
    }
}
