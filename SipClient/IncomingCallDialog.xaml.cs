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
using System.Windows.Media.Animation;
using System.Timers;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for IncomingCallDialog.xaml
    /// </summary>
    public partial class IncomingCallDialog : Window
    {
        public IncomingCallDialog()
        {
            InitializeComponent();

            this.Opacity = 0;
            // set WindowStartupLocation
            this.Left = SystemParameters.PrimaryScreenWidth - 350;
            this.Top = SystemParameters.PrimaryScreenHeight - 230;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            imgProfile.Source = PhoneWindow.ImageSourceFromBitmap(Properties.Resources.profile);

            AnimationBegin();
        }

        public void UpdateTextPanel(string phone, string name)
        {
            if (name != null)
                lblName.Content = "Имя : " + name;
            if (phone != null)
                lblPhone.Content = "Телефон : " + phone;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnClose(object sender, RoutedEventArgs e)
        {
            if (this.Call != null)
            {
                SoftPhone.TerminateCall(this.Call);
            }
            AnimationEnd();
        }

        public void AnimationBegin()
        {
            var animationBegin = new DoubleAnimation();
            animationBegin.From = this.Opacity;
            animationBegin.To = 1d;
            animationBegin.Duration = new Duration(TimeSpan.FromMilliseconds(600));
            BeginAnimation(OpacityProperty, animationBegin);
        }

        public void AnimationEnd()
        {
            var animationEnd = new DoubleAnimation();
            animationEnd.From = this.Opacity;
            animationEnd.To = 0d;
            animationEnd.Duration = new Duration(TimeSpan.FromMilliseconds(600));
            BeginAnimation(OpacityProperty, animationEnd);
            using (var timer = new Timer())
            {
                timer.Elapsed += (sender, e) =>
                {
                    this.Hide();
                };
                timer.Interval = 600;
                timer.Enabled = true;
            }
        }

        private void btnAppend(object sender, RoutedEventArgs e)
        {
            if (this.Call != null)
            {
                SoftPhone.ReceiveOrResumeCall(this.Call);
            }
            AnimationEnd();
        }

        public sipdotnet.Phone SoftPhone { get; set; }

        public sipdotnet.Call Call { get; set; }
    }
}
