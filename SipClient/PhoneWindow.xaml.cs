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
using System.Media;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Interop;

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

        public Classes.CallRecord RecordToLocalDataBase
        {
            get
            {
                if (this.recordToDataBase == null)
                    this.recordToDataBase = new Classes.CallRecord();
                return this.recordToDataBase;
            }
        }

        public static WcfConnectionService.ServiceClient WcfClient;

        public static Func<string, bool> IsPhoneNumber = (string number) => Regex.Match(number, @"^((\+7|7|8)?([0-9]){4})?([0-9]{3})?([0-9]{3})$").Success;

        private Func<string, string> GetCallId = (string sip) => Regex.Match(sip, @"[\\""](\w+)[\\""]").Groups[1].Value;

        private Func<string, string> GetPhone = (string sip) => Regex.Match(sip, @"sip:(\d+)@").Groups[1].Value;

        /// <summary>
        /// return true if s is transfer phone number
        /// </summary>
        Func<string, bool> isTransferNumber = (s) => Regex.Match(s, @"^(#){1}").Success;

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
                try
                {
                    CheckSoundDevices();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }

            }
            catch (Exception ex)
            {
                InvokeGUIThread(() => { MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace); });
            }
        }


        #region Sofphone_Events

        // Звонок завершился
        private void softphone_CallCompletedEvent(Call call)
        {
#warning НеобходимыПравки!

        }

        private void softphone_CallActiveEvent(Call call)
        {
            InvokeGUIThread(() =>
            {
                this.txtCallStatus.Text = "Входящий";
                this.PhoneIcon.Source = ImageSourceFromBitmap(Properties.Resources.call_end); // new BitmapImage(new Uri("/SipClient;component/Resources/call-end.png", UriKind.Relative));

                // Enable timer clockdown
                // TimerEnable();

                // set new call volume
                var volValue = this.volumeSlider.Value;
                softphone.GetMediaHandler.SetSpeakerSound(this.call, (float)(volValue % 100));


            });

            this.call = call;

            // write call start time
            RecordToLocalDataBase.TimeStart = DateTime.Now;
        }

        private void softphone_PhoneDisconnectedEvent()
        {
            InvokeGUIThread(() =>
            {
                //set unavailable icon and text message
                this.StatusIcon.Source = ImageSourceFromBitmap(Properties.Resources.inactive);
                txtConnectionStatus.Text = "Нет подключения!";
            });
        }

        private void softphone_PhoneConnectedEvent()
        {
            InvokeGUIThread(() =>
            {
                //set available icon and text message
                this.PhoneIcon.Source = ImageSourceFromBitmap(Properties.Resources.telephone);
                this.StatusIcon.Source = ImageSourceFromBitmap(Properties.Resources.active);
                txtConnectionStatus.Text = "Подключен!";
            });
        }

        private void softphone_IncomingCallEvent(Call incomingCall)
        {
            Parallel.Invoke(() =>
            {
                // Reject incoming call, if we have active 
                if (this.call != null)
                {
                    softphone.TerminateCall(incomingCall);

                    // write rejected call to base
                    RecordToLocalDataBase.TimeStart = DateTime.Now;
                    RecordToLocalDataBase.isRejected = true;

                    return;
                }

                // Recieve inciming call
                this.call = incomingCall;

                // write incoming call to base
                RecordToLocalDataBase.TimeStart = DateTime.Now;
                RecordToLocalDataBase.isIncoming = true;

#warning DoIncomingCallAnimationPanel
                // Do incoming call panel animation

                // Play sound
                Classes.LocalAudioPlayer.PlaySound(Properties.Resources.notification_incoming);

                //Get information about caller

                
            });

            // change icon to incoming call
            InvokeGUIThread(() =>
            {
                this.txtCallStatus.Text = "Входящий";
                this.PhoneIcon.Source = ImageSourceFromBitmap(Properties.Resources.call_end);
            });
        }

        private void GetInfoAboutCaller(ref string Name, ref string Phone, ref string Address)
        {
#warning NowUsedMysqlBase
            // use wcf connection
            //try
            //{
            //    string phone = GetPhone(this.call.GetFrom());
            //    string caller_name = GetCallId(this.call.GetFrom());
            //    string address = String.Empty;

            //    var clientInfo = WcfClient.GetClinetInformation(phone);
            //    InvokeGUIThread(() =>
            //    {
            //        //incCallWindow.SetAttributes(clientInfo.Phone, clientInfo.Name, clientInfo.Address);
            //    });
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
            //}

            // Use Mysql connection
            var tabCallerInfo = Classes.MainDataBase.GetDataTable(
                        string.Format(
                        @"select hat.order_id,hat.customer,hat.customer_name,addr.address_text
                        from orders_hat hat left join customers_addresses addr on hat.order_id=addr.order_id
                        where hat.customer = '{0}' order by date_sm desc limit 1;", Phone));
            // Set new values to caller_name , address
            if (tabCallerInfo != null && tabCallerInfo.Rows.Count > 0)
            {
                string caller_name = Convert.ToString(tabCallerInfo.Rows[0]["name"]);
                string address = Convert.ToString(tabCallerInfo.Rows[0]["address_text"]);

                InvokeGUIThread(() => {
                    this.labelName.Content = caller_name;
                    this.labelPhone.Content = Phone;
                    this.labelAddress.Content = address;
                });
            }
            else
            {
                InvokeGUIThread(() => { SetAttributes(Phone, Name, Address); });
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
                            this.StatusIcon.Source = ImageSourceFromBitmap(Properties.Resources.inactive); //new BitmapImage(new Uri("/SipClient;component/Resources/inactive.png", UriKind.Relative));
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

        #endregion

        #region PhoneWindowEvents


        #endregion
#warning ex2
        private void SetVolumes()
        {
            //var micValue = (this.micSlider.Value);
            var speakerValue = (this.volumeSlider.Value);
            //var currentSetMicValue = Math.Ceiling(softphone.GetMediaHandler.GetMicrophoneSound(call) * 100);
            var currentSetSpeakerValue = Math.Ceiling(softphone.GetMediaHandler.GetSpeakerSound(call) * 100);

            // set new mic value 
            //if (micValue - currentSetMicValue != 0)
            //{
            //    this.micSlider.Value = micValue;
            //}
            // set new speaker value 
            if (speakerValue - currentSetSpeakerValue != 0)
            {
                this.volumeSlider.Value = speakerValue;
            }
        }

        /// <summary>
        /// The controls of the Windows Form applications can only be modified on the GUI thread. This method grants access to the GUI thread.
        /// </summary>
        private void InvokeGUIThread(Action action)
        {
            Dispatcher.Invoke(action, null);
        }

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

            if (String.IsNullOrEmpty(phoneNumber))
                return;

            // Check phone number 
            if (!IsPhoneNumber(phoneNumber))
            {
                MessageBox.Show(string.Concat("Неправильный телефонный номер : '", phoneNumber, "'!"));
                return;
            }
            try
            {
                CheckSoundDevices();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            // make a call to number
            softphone.MakeCall(phoneNumber);
        }

        // изменение ползунка с громкостью
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // change graphics
            var volValue = this.volumeSlider.Value;
            InvokeGUIThread(() =>
            {
                this.volumeSlider.SelectionEnd = volValue;
            });

            // set new volume
            if (this.call != null)
            {
                softphone.GetMediaHandler.SetSpeakerSound(this.call, (float)(volValue % 100));
            }
        }

        //private void micSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    if (this.call == null)
        //        return;

        //    // change graphics
        //    //var micValue = this.micSlider.Value;
        //    InvokeGUIThread(() =>
        //    {
        //        this.micSlider.SelectionEnd = micValue;
        //    });
        //    // set new value
        //    softphone.GetMediaHandler.SetMicrophoneSound(this.call, (float)(micValue % 100));
        //}

        private void txtPhoneNumber_GotFocus(object sender, RoutedEventArgs e)
        {
            //change background color
            this.borderPhoneNumber.BorderBrush = System.Windows.Media.Brushes.Red;

            // Если в поле шаблон -> сбрасываем его
            if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
            {
                InvokeGUIThread(() =>
                {
                    txtPhoneNumber.Text = String.Empty;
                });
            }
        }

        private void txtPhoneNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            //change background color
            this.borderPhoneNumber.BorderBrush = System.Windows.Media.Brushes.Green;

            // если введеный текст не похож на шабло и не пустая строка -> выходим
            //if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
            //    return;
            //else if (string.IsNullOrEmpty(txtPhoneNumber.Text)) /* && !IsPhoneNumber(text))*/
            //{
            //    InvokeGUIThread(() =>
            //    {
            //        txtPhoneNumber.Text = _PHONE_NUMBER_HELP;
            //    });
            //}
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
                // short input keys
                case Key.NumPad0:
                case Key.D0:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        //txtPhoneNumber.Text += 0;
                    }
                    break;
                case Key.NumPad1:
                case Key.D1:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += 1;
                    }
                    break;
                case Key.NumPad2:
                case Key.D2:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += 2;
                    }
                    break;
                case Key.NumPad3:
                case Key.D3:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += 3;
                    }
                    break;
                case Key.NumPad4:
                case Key.D4:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += 4;
                    }
                    break;
                case Key.NumPad5:
                case Key.D5:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += 5;
                    }
                    break;
                case Key.NumPad6:
                case Key.D6:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += 6;
                    }
                    break;
                case Key.NumPad7:
                case Key.D7:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += 7;
                    }
                    break;
                case Key.NumPad8:
                case Key.D8:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += 8;
                    }
                    break;
                case Key.NumPad9:
                case Key.D9:
                    {
                        //clear field
                        if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                            txtPhoneNumber.Text = String.Empty;
                        //add number
                        txtPhoneNumber.Text += 9;
                    }
                    break;
                // calling to number
                case Key.Enter:
                    {
                        if (!txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                        {
                            string phoneNumber = txtPhoneNumber.Text;
                            CallTo(phoneNumber);
                        }
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

        public static ImageSource ImageSourceFromBitmap(Bitmap bitmap)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
        }

        private void buttonKeyPadButton_Click(object sender, RoutedEventArgs e)
        {
            if (txtPhoneNumber.Text.Length > 13)
                return;

            var btn = sender as Button;

            if (call == null && btn.Content.Equals('#')) return;

            //Вводим номер для перевода
            bool transferFlag = (isTransferNumber(txtPhoneNumber.Text) || btn.Content.Equals("#"));
            if (transferFlag)
            {
                InvokeGUIThread(() =>
                {
                    txtCallStatus.Text = "Трансфер на..";
                    this.PhoneIcon.Source = ImageSourceFromBitmap(Properties.Resources.telephone);
                });
            }

            if (call != null && !transferFlag)
                return;

            // Сменим btnAdditional Icon
            CheckButtonAdditionalIcon();

            // Добавим символ с кнокпки
            PutNumberWithDTMF(btn.Content.ToString());
        }

        private void CheckButtonAdditionalIcon()
        {
            if (this.txtPhoneNumber.Text == _PHONE_NUMBER_HELP || this.txtPhoneNumber.Text == string.Empty)
            {
                InvokeGUIThread(() =>
                {
                    this.btnAdditionalIcon.Source = PhoneWindow.ImageSourceFromBitmap(Properties.Resources.down);
                });
            }
            else
            {
                InvokeGUIThread(() =>
                {
                    this.btnAdditionalIcon.Source = PhoneWindow.ImageSourceFromBitmap(Properties.Resources.close);
                });
            }
        }

        private void PutNumberWithDTMF(string symb)
        {
            // Если не подсказка для ввода текста
            if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                txtPhoneNumber.Text = String.Empty;

            // Play Sound
            Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY[symb]);

            // add to phone text
            txtPhoneNumber.Text += symb;
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
                InvokeGUIThread(() => { this.SpeakerIcon.Source = ImageSourceFromBitmap(Properties.Resources.speaker_on_64x64); });
                // new BitmapImage(new Uri("/SipClient;component/Resources/speaker_on_64x64.png", UriKind.Relative)); });
            }
            else
            {
                InvokeGUIThread(() => { this.SpeakerIcon.Source = ImageSourceFromBitmap(Properties.Resources.speaker_off_64x64); });
                // new BitmapImage(new Uri("/SipClient;component/Resources/speaker_off_64x64.png", UriKind.Relative)); });
            }

            if (!MicrophoneOff)
            {
                InvokeGUIThread(() => { this.MicIcon.Source = ImageSourceFromBitmap(Properties.Resources.mic_on); });
                // new BitmapImage(new Uri("/SipClient;component/Resources/mic_on.png", UriKind.Relative)); });
            }
            else
            {
                InvokeGUIThread(() => { this.MicIcon.Source = ImageSourceFromBitmap(Properties.Resources.mic_off); });
                // new BitmapImage(new Uri("/SipClient;component/Resources/mic_off.png", UriKind.Relative)); });
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

        /// <summary>
        /// Throw Exception with message
        /// </summary>
        /// <returns></returns>
        private void CheckSoundDevices()
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
                throw new Exception(message);
            }
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
            //lblTime.Content = "00:00:00";

            //DoubleAnimation animation = new DoubleAnimation();
            //animation.From = lblTime.ActualHeight;
            //animation.To = 30;
            //animation.SpeedRatio = 2;
            //animation.Duration = TimeSpan.FromSeconds(1);
            //lblTime.BeginAnimation(Label.HeightProperty, animation);
        }

        private void AnimationTimerDecrease()
        {
            //DoubleAnimation animation = new DoubleAnimation();
            //animation.From = lblTime.ActualHeight;
            //animation.To = 0;
            //animation.SpeedRatio = 2;
            //animation.Duration = TimeSpan.FromSeconds(1);
            //lblTime.BeginAnimation(Label.HeightProperty, animation);
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
                    //lblTime.Content = timeSpan.ToString(@"mm\:ss");
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

        private void SettingsWindowClick(object sender, RoutedEventArgs e)
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

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            // принимаем звонок
            if (call != null)
            {
                softphone.ReceiveOrResumeCall(this.call);
            }
        }

        private void btnHoldOn_Click(object sender, RoutedEventArgs e)
        {
            // Если удерживаем звонок
            if (call != null)
            {
                softphone.HoldCall(this.call);
            }
        }

        private void btnReject_Click(object sender, RoutedEventArgs e)
        {
            // отклоняем звонок 
            if (call != null)
            {
                softphone.TerminateCall(this.call);
            }
        }

        private void Item_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Control).Background = System.Windows.Media.Brushes.WhiteSmoke;
        }

        private void Item_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Control).Background = null;
        }

        // Дпоплнительный контрол для поля ввода телефонного номера
        private void btnPhoneNumberAdditional_Click(object sender, RoutedEventArgs e)
        {
            if (txtPhoneNumber.Text == _PHONE_NUMBER_HELP)
            {
                ShowListToCalls();
            }
            else
            {
                txtPhoneNumber.Text = "";
            }
        }

        // Выводит список с предидущими заказами
        private void ShowListToCalls()
        {

        }

        private void txtPhoneNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.KeyDown -= new KeyEventHandler(Window_KeyDown);

            CheckButtonAdditionalIcon();
        }
    }
}
