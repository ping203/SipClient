using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using sipdotnet;
using System.Diagnostics;
using System.Xml;
using System.IO;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PhoneWindow : Window
    {
        private static Phone softphone;
        private Account account;
        private Call call;

        public static string Login { get; set; }
        public static string Host { get; set; }
        public static string Password { get; set; }
        public static bool IgnoreFlag { get; set; }

        private const string _PHONE_NUMBER_HELP = "Введите номер";

        // Record to DataBase
        private Classes.CallRecord recordToDataBase;

        public static WcfConnectionService.ServiceClient WcfClient;

        private static IncomingCallWindow incCallWindow = null;

        public PhoneWindow()
        {
            // Initialize all controls
            InitializeComponent();

            //Load configs
            SettingsWindow.LoadSettings(SettingsWindow.PathToConfigs);

            this.txtPhoneNumber.Text = _PHONE_NUMBER_HELP;
            this.SpeakerOff = false;
            this.MicrophoneOff = false;
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                // SoftPhone initialize
                InitializeSoftphone();

                // Create Client connection
                InitializeWcfClient();
            });
        }

        private void InitializeWcfClient()
        {
            WcfClient = new WcfConnectionService.ServiceClient();
            try
            {
                WcfClient.CreateConnection();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
            }
        }

        private void InitializeSoftphone()
        {
            try
            {
                account = new Account(Login, Password, Host);
                softphone = new Phone(account);

                // Add Events
                softphone.ErrorEvent += new Phone.OnError(softphone_ErrorEvent);
                softphone.IncomingCallEvent += new Phone.OnIncomingCall(softphone_IncomingCallEvent);
                softphone.PhoneConnectedEvent += new Phone.OnPhoneConnected(softphone_PhoneConnectedEvent);
                softphone.PhoneDisconnectedEvent += new Phone.OnPhoneDisconnected(softphone_PhoneDisconnectedEvent);
                softphone.CallActiveEvent += new Phone.OnCallActive(softphone_CallActiveEvent);
                softphone.CallCompletedEvent += new Phone.OnCallCompleted(softphone_CallCompletedEvent);

                // Connect to server
                softphone.Connect();

                // Set Fields
                InvokeGUIThread(() =>
                {
                    miUsername.Header = Login;
                });

                // Check sound devices
                this.CheckSoundDevices();
            }
            catch (Exception ex)
            {
                InvokeGUIThread(() => { MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace); });
            }
        }

        private void softphone_CallCompletedEvent(Call call)
        {
            InvokeGUIThread(() =>
                {
                    // Incoming call
                    if (incCallWindow != null)
                    {
                        // add record to db  
                        try
                        {
                            recordToDataBase.Phone = incCallWindow.Phone;
                            recordToDataBase.TimeEnd = DateTime.Now.ToString();
                            Classes.Records.AddRecordToDataBase(recordToDataBase);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
                        }
                        // Close incoming call window
                        incCallWindow.Close();
                        incCallWindow = null;
                    }
                    else
                    {
                        // Outcoming call
                        // add record to db  
                        try
                        {
                            recordToDataBase.isIncoming = 0;
                            recordToDataBase.Phone = txtPhoneNumber.Text;
                            recordToDataBase.TimeEnd = DateTime.Now.ToString();
                            Classes.Records.AddRecordToDataBase(recordToDataBase);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
                        }
                    }

                    // Update icons and text
                    txtPhoneNumber.Text = _PHONE_NUMBER_HELP;
                    txtCallStatus.Text = "Закончен";
                    this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/telephone.png", UriKind.Relative));

                    // Disable Timer clockdown
                    TimerDisable();
                });

            // make null current call, window, lastMessage
            this.call = null;
            //lastIncomingMessage = null;
        }

        private void softphone_CallActiveEvent(Call call)
        {
            InvokeGUIThread(() =>
            {
                this.txtCallStatus.Text = "Входящий";
                this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/call-end.png", UriKind.Relative));

                // Enable timer clockdown
                TimerEnable();
            });

            this.call = call;

            if (recordToDataBase == null)
                recordToDataBase = new Classes.CallRecord();
            recordToDataBase.TimeStart = DateTime.Now.ToString();
        }

        private void softphone_PhoneDisconnectedEvent()
        {
            InvokeGUIThread(() =>
            {
                //set unavailable icon and text message
                this.StatusIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/inactive.png", UriKind.Relative));
                txtConnectionStatus.Text = "Нет подключения!";
            });
        }

        private void softphone_PhoneConnectedEvent()
        {
            InvokeGUIThread(() =>
            {
                //set available icon and text message
                this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/telephone.png", UriKind.Relative));
                this.StatusIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/active.png", UriKind.Relative));
                txtConnectionStatus.Text = "Подключен!";
            });
        }

        private void softphone_IncomingCallEvent(Call incomingCall)
        {
            // change icon to disable
            InvokeGUIThread(() =>
            {
                this.txtCallStatus.Text = "Входящий";
                this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/call-end.png", UriKind.Relative));
            });

            // Reject incoming call, if we have active
            if (this.call != null)
            {
                softphone.TerminateCall(incomingCall);
            }
            else
            {
                this.call = incomingCall;

                // create incoming call window
                if (this.call != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        string phone = GetPhone(this.call.GetFrom());
                        string caller_name = GetCallId(this.call.GetFrom());
                        string address = String.Empty;

                        // show incoming call window
                        InvokeGUIThread(() =>
                        {
                            // Create new incoming window
                            incCallWindow = new IncomingCallWindow();
                            incCallWindow.Phone = phone;
                            incCallWindow.Name = caller_name;
                            incCallWindow.Address = address;
                            incCallWindow.Call = this.call;
                            incCallWindow.SoftPhone = softphone;
                            incCallWindow.Show();
                        });
#warning WcfCall
                        //Get data about caller
                        try
                        {
                            var clientInfo = WcfClient.GetClinetInformation(phone);
                            InvokeGUIThread(() =>
                            {
                                incCallWindow.SetAttributes(clientInfo.Phone, clientInfo.Name, clientInfo.Address);
                            });
                        }
                        catch (Exception)
                        {

                        }

                        // write incoming call record
                        if (recordToDataBase == null)
                            recordToDataBase = new Classes.CallRecord();

                        this.recordToDataBase.isIncoming = 1;
                    });
                }
            }
        }

        private void softphone_ErrorEvent(Call call, Phone.Error error)
        {
            switch (error)
            {
                case Phone.Error.CallError:
                    InvokeGUIThread(() =>
                    {
                        txtCallStatus.Text = "Ошибка вызова!";
                    });
                    break;
                case Phone.Error.LineIsBusyError:
                    InvokeGUIThread(() =>
                    {
                        txtCallStatus.Text = "Линия занята!";
                    });
                    break;
                case Phone.Error.OrderError:
                    {

                    }
                    break;
                case Phone.Error.RegisterFailed:
                    {
                        InvokeGUIThread(() =>
                {
                    //set unavailable icon and text message
                    this.StatusIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/inactive.png", UriKind.Relative));
                    txtConnectionStatus.Text = "Нет подключения!";
                });
                    }
                    break;
                case Phone.Error.UnknownError:
                    {
                        InvokeGUIThread(() =>
                        {
                            txtCallStatus.Text = "Неизвестная ошибка!";
                        });
                    }
                    break;
            }
        }

#warning ex2
        private void SetVolumes()
        {
            var micValue = (this.micSlider.Value);
            var speakerValue = (this.volumeSlider.Value);
            var currentSetMicValue = Math.Ceiling(softphone.GetMediaHandler.GetMicrophoneSound(call) * 100);
            var currentSetSpeakerValue = Math.Ceiling(softphone.GetMediaHandler.GetSpeakerSound(call) * 100);

            // set new mic value 
            if (micValue - currentSetMicValue != 0)
            {
                this.micSlider.Value = micValue;
            }
            // set new speaker value 
            if (speakerValue - currentSetSpeakerValue != 0)
            {
                this.volumeSlider.Value = speakerValue;
            }
        }

        private Func<string, bool> IsPhoneNumber = (string number) => Regex.Match(number, @"^((\+7|7|8)+([0-9]){4})?([0-9]{3})?([0-9]{3})$").Success;

        private Func<string, string> GetCallId = (string sip) => Regex.Match(sip, @"[\\""](\w+)[\\""]").Groups[1].Value;

        private Func<string, string> GetPhone = (string sip) => Regex.Match(sip, @"sip:(\d+)@").Groups[1].Value;

        /// <summary>
        /// Send message to Orders
        /// </summary>
        //public static void SendMessageToOrders()
        //{
        //    //Формируем сообщение и отправляем его
        //    using (var msg = new System.Messaging.Message())
        //    {
        //        //create message
        //        MSQ.Message msq_message = null;
        //        //use latest message
        //        if (lastIncomingMessage != null)
        //        {
        //            //Set new behaviour
        //            lastIncomingMessage.BehaviourFlags.isCreateNewOrder = CreateNewOrderFlag;
        //            //set link to message
        //            msq_message = lastIncomingMessage;
        //        }
        //        else //or create new message
        //        {
        //            string phone = string.Empty;
        //            if (incCallWindow != null)
        //                phone = incCallWindow.Phone;

        //            msq_message = new MSQ.Message();
        //            msq_message.BehaviourFlags = new MSQ.Behaviour() { isFinded = false, isCreateNewOrder = false };
        //            msq_message.CustomerInfo = new MSQ.Customer() { Addres = String.Empty, Name = String.Empty, PhoneNumber = phone };
        //        }
        //        //Create queue message with guaranteed delivery
        //        msg.Body = msq_message;
        //        msg.Recoverable = true;
        //        msg.Formatter = new BinaryMessageFormatter();
        //        //Send to sipClient queue
        //        SipClientQueue.Send(msg);
        //    }
        //}

        /// <summary>
        ///  Click on the phone board buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnActionClick(object sender, EventArgs e)
        {
            var btn = sender as Button;

            if (btn == null)
                return;

            // add to phone number collection
            if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
            {
                txtPhoneNumber.Text = String.Empty;
            }
            txtPhoneNumber.Text += Convert.ToString(btn.Content).Trim();
        }

        /// <summary>
        /// The controls of the Windows Form applications can only be modified on the GUI thread. This method grants access to the GUI thread.
        /// </summary>
        private void InvokeGUIThread(Action action)
        {
            Dispatcher.Invoke(action, null);
        }

        /// <summary>
        /// return true if s is transfer phone number
        /// </summary>
        Func<string, bool> isTransferNumber = (s) => Regex.Match(s, @"^(#){1}").Success;

        /// <summary>
        ///  Нажатие на кнопку соединить\ разъединить
        /// </summary>
        private void btnConnectOrReject_Click(object sender, RoutedEventArgs e)
        {
            switch (softphone.CurrentLineState)
            {
                case Phone.LineState.Busy:
                    {
                        // Если введеный номер является номер перевода -> переводим на указанный номер
                        if (isTransferNumber(txtPhoneNumber.Text))
                        {
                            string number = txtPhoneNumber.Text.Remove(0, 1); // remove '#'
                            TransferTo(number);
                            return;
                        }
                        // Разрываем активное соединение
                        else if (this.call != null)
                        {
                            softphone.TerminateCall(call);
                        }
                    }
                    break;
                case Phone.LineState.Free:
                    {
                        // Вызов номера
                        string phoneNumber = txtPhoneNumber.Text;
                        CallTo(phoneNumber);
                    }
                    break;
            }
        }

        /// <summary>
        /// Вызов номера по телефону
        /// </summary>
        /// <param name="phoneNumber"></param>
        private void CallTo(string phoneNumber)
        {
            if (this.call != null)
                return;

            // Check phone number 
            if (!IsPhoneNumber(phoneNumber) || String.IsNullOrEmpty(phoneNumber))
            {
                MessageBox.Show(string.Concat("Неправильный телефонный номер : '", phoneNumber, "'!"));
                return;
            }
            // make a call to number
            softphone.MakeCall(phoneNumber);
        }

        // изменение ползунка с громкостью
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.call == null)
                return;

            // change graphics
            var volValue = this.volumeSlider.Value;
            InvokeGUIThread(() =>
            {
                this.volumeSlider.SelectionEnd = volValue;
            });
            // set new value
            softphone.GetMediaHandler.SetSpeakerSound(this.call, (float)(volValue % 100));
        }

        private void micSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.call == null)
                return;

            // change graphics
            var micValue = this.micSlider.Value;
            InvokeGUIThread(() =>
            {
                this.micSlider.SelectionEnd = micValue;
            });
            // set new value
            softphone.GetMediaHandler.SetMicrophoneSound(this.call, (float)(micValue % 100));
        }

        private void txtPhoneNumber_GotFocus(object sender, RoutedEventArgs e)
        {
            string text = (txtPhoneNumber.Text).Trim();
            // Если в поле шаблон -> сбрасываем его
            if (text.Equals(_PHONE_NUMBER_HELP))
            {
                InvokeGUIThread(() =>
                {
                    txtPhoneNumber.Text = String.Empty;
                });
            }
        }

        private void txtPhoneNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            string text = (txtPhoneNumber.Text).Trim();
            // если введеный текст не похож на шабло и не пустая строка -> выходим
            if (text.Equals(_PHONE_NUMBER_HELP))
                return;
            else if (string.IsNullOrEmpty(text) && !IsPhoneNumber(text))
            {
                InvokeGUIThread(() =>
                {
                    txtPhoneNumber.Text = _PHONE_NUMBER_HELP;
                });
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                // Remove last characters
                case Key.Back:
                case Key.Delete:
                    if (!txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP) && !(txtPhoneNumber.Text.Length == 0))
                    {
                        txtPhoneNumber.Text = txtPhoneNumber.Text.Remove(txtPhoneNumber.Text.Length - 1, 1);
                    }
                    break;
                // short input
                case Key.NumPad0:
                case Key.NumPad1:
                case Key.NumPad2:
                case Key.NumPad3:
                case Key.NumPad4:
                case Key.NumPad5:
                case Key.NumPad6:
                case Key.NumPad7:
                case Key.NumPad8:
                case Key.NumPad9:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += (Convert.ToInt32(e.Key) - 74);
                    }
                    break;
                // short input 
                case Key.D0:
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += (Convert.ToInt16(e.Key) - 34);
                    }
                    break;
                // calling to number
                case Key.Enter:
                    {
                        string phoneNumber = txtPhoneNumber.Text;
                        CallTo(phoneNumber);
                    }
                    break;
            }
        }

        private void btnMinimizeClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void btnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private static bool createNewOrderFlag;

        /// <summary>
        /// Return flag and unset him
        /// </summary>
        public static bool CreateNewOrderFlag
        {
            get
            {
                bool oldValue = createNewOrderFlag;
                createNewOrderFlag = false;
                return oldValue;
            }
            set
            {
                createNewOrderFlag = value;
            }
        }

        private void TransferTo(string transferNumber)
        {
            // Если введеный номер не правильный или [object]call == nil -> выход
            if (this.call == null || string.IsNullOrEmpty(transferNumber))
                return;

            softphone.MakeTransfer(this.call, transferNumber);

            InvokeGUIThread(() =>
            {
                txtCallStatus.Text = "Трансфер на :" + transferNumber;
            });
        }

        private void buttonKeyPadButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            if (btn == null)
                return;

            if (call == null && btn.Content.Equals('#')) return;

            //Вводим номер для перевода
            bool transferFlag = (isTransferNumber(txtPhoneNumber.Text) || btn.Content.Equals("#"));
            if (transferFlag)
            {
                InvokeGUIThread(() =>
                {
                    txtCallStatus.Text = "Трансфер на..";
                    this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/telephone.png", UriKind.Relative));
                });
            }

            if (call != null && !transferFlag)
                return;

            // Если не подсказка для ввода текста
            if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                txtPhoneNumber.Text = String.Empty;

            // Добавим символ с кнокпки
            txtPhoneNumber.Text += btn.Content.ToString().Trim();
        }

        public bool SpeakerOff { get; set; }
        public bool MicrophoneOff { get; set; }

        private void btnVolumeOffOn(object sender, RoutedEventArgs e)
        {
            this.SpeakerOff = !SpeakerOff;
            ChangeIcons();
        }

        private void btnMicOffOn(object sender, RoutedEventArgs e)
        {
            this.MicrophoneOff = !MicrophoneOff;
#warning ex3
            softphone.GetMediaHandler.MicrophoneEnable(!MicrophoneOff);
            ChangeIcons();
        }

        private void ChangeIcons()
        {
            if (!SpeakerOff)
            {
                InvokeGUIThread(() => { this.SpeakerIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/speaker_on_64x64.png", UriKind.Relative)); });
            }
            else
            {
                InvokeGUIThread(() => { this.SpeakerIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/speaker_off_64x64.png", UriKind.Relative)); });
            }

            if (!MicrophoneOff)
            {
                InvokeGUIThread(() => { this.MicIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/mic_on.png", UriKind.Relative)); });
            }
            else
            {
                InvokeGUIThread(() => { this.MicIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/mic_off.png", UriKind.Relative)); });
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // close all conenctions
            softphone.Disconnect();
            //OrdersQueue.ReceiveCompleted -= new ReceiveCompletedEventHandler(OrdersQueue_ReceiveCompleted);
            //SipClientQueue.Dispose();
            //OrdersQueue.Dispose();
            // application shutdown
            App.Current.Shutdown();
        }

        private bool CheckSoundDevices()
        {
#warning ex4
            var soundDevs = softphone.GetMediaHandler.GetAvailableSoundDevices;
            var playbackDev = soundDevs.Where(isSpeaker).FirstOrDefault();
            var recordDev = soundDevs.Where(isMicrophone).FirstOrDefault();

            bool notGood = true;
            if (notGood = (playbackDev == null || recordDev == null))
            {
                string message = "Отсутствует устройство ";
                if (playbackDev == null)
                    message += ": Воспроизведения ";
                if (recordDev == null)
                    message += ": Записи ";
                message += "!\nПроверьте подключение этих устройств!";
                MessageBox.Show(message, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return !notGood;
        }

        private Func<string, bool> isMicrophone = (device) => Regex.Match(device, "(Микрофон)").Success;
        private Func<string, bool> isSpeaker = (device) => Regex.Match(device, "(Динамики)").Success;

        // Show user call statistic
        private void miUsername_Click(object sender, RoutedEventArgs e)
        {
            UserStat userStat = UserStat.GetInstance;
            userStat.ReloadTable();
            userStat.Show();
        }

        private DispatcherTimer timer;
        private TimeSpan timeSpan;

        private void AnimationTimerGrow()
        {
            lblTime.Content = "00:00:00";

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = lblTime.ActualHeight;
            animation.To = 30;
            animation.SpeedRatio = 2;
            animation.Duration = TimeSpan.FromSeconds(1);
            lblTime.BeginAnimation(Label.HeightProperty, animation);
        }

        private void AnimationTimerDecrease()
        {
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = lblTime.ActualHeight;
            animation.To = 0;
            animation.SpeedRatio = 2;
            animation.Duration = TimeSpan.FromSeconds(1);
            lblTime.BeginAnimation(Label.HeightProperty, animation);
        }

        internal void StartTimer()
        {
            if (timer == null)
                timer = new DispatcherTimer();

            timeSpan = new TimeSpan();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 0010);
            timer.Tick += ((sender, e) =>
            {
                InvokeGUIThread(() =>
                {
                    timeSpan += timer.Interval;
                    lblTime.Content = timeSpan.ToString(@"mm\:ss\:ff");
                });
            });
            timer.Start();
        }

        internal void StopTimer()
        {
            if (timer == null)
                return;

            timer.Stop();
        }

        internal void TimerEnable()
        {
            // load animation
            AnimationTimerGrow();
            // run timer
            StartTimer();
        }

        internal void TimerDisable()
        {
            StopTimer();
            DispatcherTimer animTimer = new DispatcherTimer();
            animTimer.Interval = new TimeSpan(0, 0, 2);
            animTimer.Tick += (sender, e) =>
            {
                AnimationTimerDecrease();
                animTimer.Stop();
            };
            animTimer.Start();
        }

        private void LoginMenuClick(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            if (settings.ShowDialog() == true)
            {
                Task.Factory.StartNew(() =>
                {
                    SettingsWindow.LoadSettings(SettingsWindow.PathToConfigs);
                    InitializeSoftphone(); ;
                });
            }
        }
    }
}
