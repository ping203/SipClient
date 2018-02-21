using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading;
using Ozeki.VoIP;
using Ozeki.Media;
using Ozeki.Media.MediaHandlers;
using Ozeki.VoIP.SDK;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PhoneWindow : Window
    {
        private ISoftPhone _softPhone;
        private IPhoneLine _phoneLine;
        private RegState _phoneLineState;
        private IPhoneCall call;

        public static Microphone microphone;
        public static Speaker speaker;
        private MediaConnector connector;
        private PhoneCallAudioSender mediaSender;
        private PhoneCallAudioReceiver mediaReceiver;

        private const string _PHONE_NUMBER_HELP = "Введите номер";

        private bool _incomingCall;

        public PhoneWindow()
        {
            // Initialize all controls
            InitializeComponent();

            InitializeControlEvents();

            txtPhoneNumber.Text = _PHONE_NUMBER_HELP;
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            microphone = Microphone.GetDefaultDevice();
            speaker = Speaker.GetDefaultDevice();
            connector = new MediaConnector();
            mediaSender = new PhoneCallAudioSender();
            mediaReceiver = new PhoneCallAudioReceiver();

            speaker.Volume = (float)volumeSlider.Value;

            miUsername.Header = Login;

            // Запускаем иниицализацию в параллельном потоке
            ThreadPool.QueueUserWorkItem(InitializeSoftphone);
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
        }

        private void InitializeSoftphone(object state)
        {
            try
            {
                _softPhone = SoftPhoneFactory.CreateSoftPhone(SoftPhoneFactory.GetLocalIP(), 5000, 15000);

                _phoneLine = _softPhone.CreatePhoneLine(new SIPAccount(true, Login, Login, Login, Password, "192.168.0.5"));
                _phoneLine.RegistrationStateChanged += _phoneLine_RegistrationStateChanged;

                _softPhone.IncomingCall += _softPhone_IncomingCall;

                _softPhone.RegisterPhoneLine(_phoneLine);

                _incomingCall = false;

                ConnectMedia();
            }
            catch (Exception ex)
            {
                InvokeGUIThread(() => { MessageBox.Show(ex.Message); });
            }
        }

        // Обработка изменения состояния соединения с сервером
        private void _phoneLine_RegistrationStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            _phoneLineState = e.State;

            if (_phoneLineState == RegState.Error || _phoneLineState == RegState.NotRegistered)
            {
                InvokeGUIThread(() =>
                {
                    //set unavailable icon and text message
                    var source = new Uri("/SipClient;component/Resources/inactive.png", UriKind.Relative);
                    this.StatusIcon.Source = new BitmapImage(source);
                    txtConnectionStatus.Text = "Нет подключения!";
                });
            }
            else if (_phoneLineState == RegState.RegistrationSucceeded) // Online!
            {
                InvokeGUIThread(() =>
                {
                    //set available icon and text message
                    this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/telephone.png", UriKind.Relative));
                    this.StatusIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/active.png", UriKind.Relative));
                    txtConnectionStatus.Text = "Подключен!";
                });
            }
        }

        // Check number
        private bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"^((\+7|7|8)+([0-9]){4})?([0-9]{6})$").Success;
        }

        // Обработка входящего звонка
        private void _softPhone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            var userName = e.Item.DialInfo.Dialed;
            // Выводим инфу о звонящем
            InvokeGUIThread(() =>
            {
                this.txtCallStatus.Text = "Входящий";
            });

            var incomingWindow = new IncomingCallWindow();
            incomingWindow.Call = call;
            incomingWindow.RingingUser = e.Item.DialInfo;
            incomingWindow.Show();

            call = e.Item;
            WireUpCallEvents();
            _incomingCall = true;
        }

        // Вызов события изменения состояния звонка
        private void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            if (e.State == CallState.Answered) // звонок принят
            {
                StartDevices();
                mediaSender.AttachToCall(call);
                mediaReceiver.AttachToCall(call);

                InvokeGUIThread(() =>
                {
                    txtCallStatus.Text = "Устанавливается соединение";
                    this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/call-end.png", UriKind.Relative));
                });
            }
            else if (e.State == CallState.InCall) // на линии
            {
                InvokeGUIThread(() =>
                {
                    txtCallStatus.Text = "Дозвон";
                });
                StartDevices();
            }

            if (e.State == CallState.LocalHeld || e.State == CallState.InactiveHeld) // неактивен или удерживается
            {
                StopDevices();
                InvokeGUIThread(() =>
                {
                    txtPhoneNumber.Text = String.Empty;
                    txtCallStatus.Text = "Удерживается на линии";
                    this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/telephone.png", UriKind.Relative));
                });
            }
            else
            {
                //InvokeGUIThread(() => { btn_Hold.Text = "Hold"; });
            }

            if (e.State.IsCallEnded()) // звонок окончен
            {
                StopDevices();

                mediaSender.Detach();
                mediaReceiver.Detach();

                WireDownCallEvents();

                call = null;

                InvokeGUIThread(() => { txtPhoneNumber.Text = String.Empty; });
            }
        }

        // Вывод инфы о пользователе
        //void ShowUserInfos(UserInfo otherParty)
        //{
        //    InvokeGUIThread(() =>
        //        {
        //            //tb_OtherPartyUserName.Text = otherParty.UserName;
        //            //tb_OtherPartyRealName.Text = otherParty.RealName;
        //            //tb_OtherPartyCountry.Text = otherParty.Country;
        //            //tb_OtherPartyNote.Text = otherParty.Note;
        //        });
        //}

        /// <summary>
        ///  Click on the phone board buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnActionClick(object sender, EventArgs e)
        {
            var btn = sender as Button;

            if (call != null)
                return;

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
        /// Запуск медиа девайсов
        /// </summary>
        private void StartDevices()
        {
            if (microphone != null)
                microphone.Start();

            if (speaker != null)
                speaker.Start();
        }

        /// <summary>
        /// Отсановка медиа девайсов
        /// </summary>
        private void StopDevices()
        {
            if (microphone != null)
                microphone.Stop();

            if (speaker != null)
                speaker.Stop();
        }

        /// <summary>
        /// Соединяем медиа девайсы
        /// </summary>
        private void ConnectMedia()
        {
            if (microphone != null)
                connector.Connect(microphone, mediaSender);

            if (speaker != null)
                connector.Connect(mediaReceiver, speaker);
        }

        private void WireUpCallEvents()
        {
            call.CallStateChanged += (call_CallStateChanged);
        }

        private void WireDownCallEvents()
        {
            call.CallStateChanged -= (call_CallStateChanged);
        }

        /// <summary>
        ///  Передача команды в главный поток
        /// </summary>
        private void InvokeGUIThread(Action action)
        {
            Dispatcher.Invoke(action);
        }

        /// <summary>
        ///  Нажатие на кнопку соединить\ разъединить
        /// </summary>
        private void btnConnectOrReject_Click(object sender, RoutedEventArgs e)
        {
            // отвечаем на звонок
            if (_incomingCall)
            {
                _incomingCall = false;
                call.Answer();

                return;
            }
            // Если не удалось создать подключение
            if (call != null)
                return;
            // Проверяем соединение
            if (_phoneLineState != RegState.RegistrationSucceeded)
            {
                InvokeGUIThread(() =>
                {
                    MessageBox.Show("Клиент оффлайн. Звонок не возможен!");
                });
                return;
            }
            // Выполняем набор
            if (!String.IsNullOrEmpty(txtPhoneNumber.Text))
            {
                var phoneNumber = txtPhoneNumber.Text;

                // Check phone number 
                if (!IsPhoneNumber(phoneNumber))
                {
                    MessageBox.Show(string.Concat("Неправильный телефонный номер : '", phoneNumber, "'!"));
                    return;
                }

                // make a call to number
                call = _softPhone.CreateCallObject(_phoneLine, phoneNumber);
                WireUpCallEvents();
                call.Start();
            }
        }

        /// <summary>
        /// Удерживание на линии
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_HangUp_Click(object sender, EventArgs e)
        {
            if (call != null)
            {
                if (_incomingCall && call.CallState == CallState.Ringing)
                {
                    call.Reject();
                }
                else
                {
                    call.HangUp();
                }
                _incomingCall = false;
                call = null;
            }
            txtPhoneNumber.Text = string.Empty;
        }

        /// <summary>
        /// Переадрессация входящих
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Transfer_Click(object sender, EventArgs e)
        {
            string transferTo = "1001";

            if (call == null)
                return;

            if (string.IsNullOrEmpty(transferTo))
                return;

            if (call.CallState != CallState.InCall)
                return;

            call.BlindTransfer(transferTo);
            InvokeGUIThread(() =>
            {
                //tb_Display.Text = "Transfering to:" + transferTo;
            });
        }

        private void btn_Hold_Click(object sender, EventArgs e)
        {
            if (call != null)
                call.ToggleHold();
        }

        private bool isVolumeOff;

        // изменение ползунка с громкостью
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // change graphics
            if (volumeSlider.Value == 0)
            {
                isVolumeOff = true;
                InvokeGUIThread(() =>
                {
                    var source = new Uri(@"/SipClient;component/Resources/speaker_off_64x64.png", UriKind.Relative);
                    this.SoundIcon.Source = new BitmapImage(source);
                });
            }
            else if (volumeSlider.Value > 0 && isVolumeOff)
            {
                InvokeGUIThread(() =>
                {
                    var source = new Uri(@"/SipClient;component/Resources/speaker_on_64x64.png", UriKind.Relative);
                    this.SoundIcon.Source = new BitmapImage(source);
                });
            }
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Вешаем трубку и разрываем соединение
            if (call != null)
            {
                call.HangUp();
                call = null;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Избегаем повторного ввода
            if (txtPhoneNumber.IsFocused)
                return;

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
            this.DragMove();
        }

        public string Login { get; set; }

        public string Password { get; set; }
    }
}
