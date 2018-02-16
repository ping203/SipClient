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
using System.Windows.Threading;
using System.Threading.Tasks;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ISoftPhone _softPhone;
        private IPhoneLine _phoneLine;
        private RegState _phoneLineState;
        private IPhoneCall _call;

        private Microphone _microphone;
        private Speaker _speaker;
        private MediaConnector _connector;
        private PhoneCallAudioSender _mediaSender;
        private PhoneCallAudioReceiver _mediaReceiver;

        private const string _PHONE_NUMBER_HELP = "Введите номер";

        private bool _incomingCall;

        public MainWindow()
        {
            // Initialize all controls
            InitializeComponent();

            InitializeControlEvents();

            txtPhoneNumber.Text = _PHONE_NUMBER_HELP;
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _microphone = Microphone.GetDefaultDevice();
            _speaker = Speaker.GetDefaultDevice();
            _connector = new MediaConnector();
            _mediaSender = new PhoneCallAudioSender();
            _mediaReceiver = new PhoneCallAudioReceiver();

            // Запускаем иниицализацию в параллельном потоке
            Task.Factory.StartNew(() =>
            {
                InitializeSoftphone();
            });

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

        }

        void InitializeSoftphone()
        {
            try
            {
                _softPhone = SoftPhoneFactory.CreateSoftPhone(SoftPhoneFactory.GetLocalIP(), 5700, 5750);

                SIPAccount sa = new SIPAccount(true, "test", "test", "test", "testpass", "192.168.0.5");

                _phoneLine = _softPhone.CreatePhoneLine(sa);
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
        void _phoneLine_RegistrationStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            _phoneLineState = e.State;

            if (_phoneLineState == RegState.Error || _phoneLineState == RegState.NotRegistered)
            {
                InvokeGUIThread(() =>
                {
                    //set unavailable icon and text message
                    var source = new Uri("/SipClient;component/Resources/inactive.png", UriKind.Relative);
                    this.StatusIcon.Source = new BitmapImage(source);
                    textStatus.Text = "Нет подключения!";
                });
            }
            else if (_phoneLineState == RegState.RegistrationSucceeded) // Online!
            {
                InvokeGUIThread(() =>
                {
                    //set available icon and text message
                    var source = new Uri("/SipClient;component/Resources/active.png", UriKind.Relative);
                    this.StatusIcon.Source = new BitmapImage(source);
                    textStatus.Text = "Подключен!";
                });
            }
        }

        // Check number
        bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"^(\+[0-9]{9})$").Success;
        }

        // Обработка входящего звонка
        void _softPhone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            var userName = e.Item.DialInfo.Dialed;
            // Вывод сообщений
            InvokeGUIThread(() => { });

            _call = e.Item;
            WireUpCallEvents();
            _incomingCall = true;

            // Выводим инфу о звонящем
            //ShowUserInfos(_otherParty);
        }

        // Вызов события изменения состояния звонка
        void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            if (e.State == CallState.Answered) // звонок принят
            {
                StartDevices();
                _mediaSender.AttachToCall(_call);
                _mediaReceiver.AttachToCall(_call);

                InvokeGUIThread(() =>
                {
                    var source = new Uri("/SipClient;component/Resources/call-end.png", UriKind.Relative);
                    this.PhoneIcon.Source = new BitmapImage(source);
                });
            }
            else if (e.State == CallState.InCall) // на линии
            {
                StartDevices();
            }

            if (e.State == CallState.LocalHeld || e.State == CallState.InactiveHeld) // неактивен или удерживается
            {
                StopDevices();
                InvokeGUIThread(() =>
                {
                    txtPhoneNumber.Text = String.Empty;
                    var source = new Uri("/SipClient;component/Resources/telephone.png", UriKind.Relative);
                    this.PhoneIcon.Source = new BitmapImage(source);
                });
            }
            else
            {
                //InvokeGUIThread(() => { btn_Hold.Text = "Hold"; });
            }

            if (e.State.IsCallEnded()) // звонок окончен
            {
                StopDevices();

                _mediaSender.Detach();
                _mediaReceiver.Detach();

                WireDownCallEvents();

                _call = null;

                // линия свободна - можем звонить
                InvokeGUIThread(() =>
                {
                    txtPhoneNumber.Text = String.Empty;
                    var source = new Uri("/SipClient;component/Resources/telephone.png", UriKind.Relative);
                    this.PhoneIcon.Source = new BitmapImage(source);
                });
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
        void btnActionClick(object sender, EventArgs e)
        {
            var btn = sender as Button;

            if (_call != null)
                return;

            if (btn == null)
                return;

            // add to phone number collection
            txtPhoneNumber.Text += Convert.ToString(btn.Content).Trim();
        }

        /// <summary>
        /// Запуск медиа девайсов
        /// </summary>
        void StartDevices()
        {
            if (_microphone != null)
                _microphone.Start();

            if (_speaker != null)
                _speaker.Start();
        }

        /// <summary>
        /// Отсановка медиа девайсов
        /// </summary>
        void StopDevices()
        {
            if (_microphone != null)
                _microphone.Stop();

            if (_speaker != null)
                _speaker.Stop();
        }

        /// <summary>
        /// Соединяем медиа девайсы
        /// </summary>
        void ConnectMedia()
        {
            if (_microphone != null)
                _connector.Connect(_microphone, _mediaSender);

            if (_speaker != null)
                _connector.Connect(_mediaReceiver, _speaker);
        }

        void WireUpCallEvents()
        {
            _call.CallStateChanged += (call_CallStateChanged);
        }

        void WireDownCallEvents()
        {
            _call.CallStateChanged -= (call_CallStateChanged);
        }

        /// <summary>
        ///  Передача команды в главный поток
        /// </summary>
        void InvokeGUIThread(Action action)
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
                _call.Answer();

                return;
            }
            // Если не удалось создать подключение
            if (_call != null)
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
                _call = _softPhone.CreateCallObject(_phoneLine, phoneNumber);
                WireUpCallEvents();
                _call.Start();
            }
        }

        /// <summary>
        /// Удерживание на линии
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btn_HangUp_Click(object sender, EventArgs e)
        {
            if (_call != null)
            {
                if (_incomingCall && _call.CallState == CallState.Ringing)
                {
                    _call.Reject();
                }
                else
                {
                    _call.HangUp();
                }
                _incomingCall = false;
                _call = null;
            }
            txtPhoneNumber.Text = string.Empty;
        }

        /// <summary>
        /// Переадрессация входящих
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btn_Transfer_Click(object sender, EventArgs e)
        {
            string transferTo = "1001";

            if (_call == null)
                return;

            if (string.IsNullOrEmpty(transferTo))
                return;

            if (_call.CallState != CallState.InCall)
                return;

            _call.BlindTransfer(transferTo);
            InvokeGUIThread(() =>
            {
                //tb_Display.Text = "Transfering to:" + transferTo;
            });
        }

        void btn_Hold_Click(object sender, EventArgs e)
        {
            if (_call != null)
                _call.ToggleHold();
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
    }
}
