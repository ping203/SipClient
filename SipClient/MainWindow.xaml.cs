using System;
using System.Linq;
using System.Text;
using System.Windows;
using Ozeki.VoIP;
using System.ComponentModel;
using Ozeki.Media;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Softphone _softphone; // softphone object
        private static StringBuilder numberToDial = new StringBuilder(13);
        private bool callEndedFlag; 


        public MainWindow()
        {
            // Initialize all controls
            InitializeComponent();

            InitializeControlEvents();

            // Initialize sofphone components
            InitSoftphone();

            //ReadRegisterInfos();
        }

        // Создание событий для кнопок и т.д
        private void InitializeControlEvents()
        {
            // set button with number click event
            btnAction0.Click += btnActionClick;
            btnAction1.Click += btnActionClick;
            btnAction2.Click += btnActionClick;
            btnAction3.Click += btnActionClick;
            btnAction4.Click += btnActionClick;
            btnAction5.Click += btnActionClick;
            btnAction6.Click += btnActionClick;
            btnAction7.Click += btnActionClick;
            btnAction8.Click += btnActionClick;
            btnAction9.Click += btnActionClick;
            // set asterisk and cell button event
            btnActionAsterisk.Click += btnActionClick;
            btnActionCell.Click += btnActionClick;

            //TextBoxEvents
            txtPhoneNumber.TextChanged += new TextChangedEventHandler(txtPhoneNumber_TextChanged);
            txtPhoneNumber.MouseLeftButtonDown += new MouseButtonEventHandler(txtPhoneNumber_MouseLeftButtonDown);
        }

        // Click on the phone board buttons
        private void btnActionClick(object sender, EventArgs e)
        {
            // add to phone number collection
            numberToDial.Append((sender as Button).Content);
            txtPhoneNumber.Text = numberToDial.ToString();
        }

        // Нажатие на кнопку соединить\ разъединить
        private void btnConnectOrReject_Click(object sender, RoutedEventArgs e)
        {
            // Check phone number 
            if(!IsPhoneNumber(numberToDial.ToString())){
                MessageBox.Show(string.Concat("Неправильный телефонный номер : '", numberToDial, "'!"));
                return;
            }

            // make a call to number
            MakeCall(numberToDial);
        }

        private void MakeCall(StringBuilder numberToDial)
        {
            //Clear numberToDial
            string callNumber = numberToDial.ToString();
            numberToDial.Clear();

            _softphone.StartCall(callNumber);
        }

        // Check number
        public static bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"^(\+[0-9]{9})$").Success;
        }

        /// <summary>
        /// Initializes the softphone logic and subscribes to its events to get notifications from it.
        /// (eg. the registration state of the phone line has changed or an incoming call received)
        /// </summary>
        private void InitSoftphone()
        {
            _softphone = new Softphone();
            
            //Register phone
            Register();

            // Event handlers
            _softphone.PhoneLineStateChanged += _softphone_RegStateChanged;  // соединение с asterisk
            _softphone.CallStateChanged += _softphone_CallStateChanged;     // статус соединения с клиентом
        }

        /// <summary>
        /// This will be called when the state of the call has changed. (eg. ringing, answered, rejected)
        /// </summary>
        private void _softphone_CallStateChanged(object sender, CallStateChangedArgs e)
        {
#warning CallStateChangeNotificate
            // вывод состояни звонка
            if (e.State == CallState.Completed)
            {
                txtPhoneNumber.Text = String.Empty;
            }
            // установка флага свободной линии
            callEndedFlag = e.State.IsCallEnded();
        }

        /// <summary>
        /// This will be called when the registration state of the phone line has changed.
        /// </summary>
        private void _softphone_RegStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            if (e.State == RegState.Error || e.State == RegState.NotRegistered)
            {
                //set unavailable icon and text message
                var source = new Uri("/SipClient;component/Resources/inactive.png", UriKind.Relative);
                this.StatusIcon.Source = new BitmapImage(source);
                textStatus.Text = "Нет подключения!";
            }
            else if (e.State == RegState.RegistrationSucceeded) // Online!
            {
                //set available icon and text message
                var source = new Uri("/SipClient;component/Resources/active.png", UriKind.Relative);
                this.StatusIcon.Source = new BitmapImage(source);
                textStatus.Text = "На линии!";
            }
        }

        private void Register()
        {
            // When every information has been given, we are trying to register to the PBX with the softphone's Register() method.
            var registrationRequired = true;
            var displayName = "test";
            var userName = "test";
            var authenticationId = "test";
            var registerPassword = "testpass";
            var domainHost = "192.168.0.5";
            var domainPort = 5060;

            _softphone.Register(registrationRequired, displayName, userName, authenticationId, registerPassword,
                                 domainHost, domainPort);
        }

        // изменение ползунка с громкостью
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // change graphics
            if (volumeSlider.Value == 0)
            {
                var source = new Uri(@"/SipClient;component/Resources/speaker_off_64x64.png", UriKind.Relative);
                this.SoundIcon.Source = new BitmapImage(source);
            }
            else
            {
                var source = new Uri(@"/SipClient;component/Resources/speaker_on_64x64.png", UriKind.Relative);
                this.SoundIcon.Source = new BitmapImage(source);
            }
        }
        
        // обработка ввода текста
        private void txtPhoneNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtPhoneNumber.Text))
            {
                txtPhoneNumber.Text = "Введите номер";
            }
        }

        private void txtPhoneNumber_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (String.IsNullOrEmpty(numberToDial.ToString()))
            {
                txtPhoneNumber.Text = String.Empty;
            }
        }
    }
}
