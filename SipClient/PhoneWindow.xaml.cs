using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Ozeki.Media;
using Ozeki.Media.MediaHandlers;
using Ozeki.VoIP;
using Ozeki.VoIP.SDK;
using System.Messaging;
using System.Runtime.Serialization.Formatters.Binary;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PhoneWindow : Window
    {
        ISoftPhone softPhone;
        IPhoneLine phoneLine;
        IPhoneCall call;
        private Microphone microphone = Microphone.GetDefaultDevice();
        private Speaker speaker = Speaker.GetDefaultDevice();
        MediaConnector connector = new MediaConnector();
        PhoneCallAudioSender mediaSender = new PhoneCallAudioSender();
        PhoneCallAudioReceiver mediaReceiver = new PhoneCallAudioReceiver();
        int currentDtmfSignal;

        private const string _PHONE_NUMBER_HELP = "Введите номер";

        private static MessageQueue SipClientQueue;
        private static MessageQueue OrdersQueue;

        public PhoneWindow()
        {
            // Initialize all controls
            InitializeComponent();

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

            //Инициализиурем погдключение к очереди сообщений
            try
            {
                SipClientQueue = MSQ.MessageQueueFactory.GetSipClientQueue;
                OrdersQueue = MSQ.MessageQueueFactory.GetOrdersQueue;

                OrdersQueue.ReceiveCompleted += new ReceiveCompletedEventHandler(OrdersQueue_ReceiveCompleted);
                OrdersQueue.BeginReceive();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        IncomingCallWindow incCallWindow = null;
        private static MSQ.Message lastIncomingMessage;

        // Обрабатываем входящее сообщение в парраллельном потоке
        private void ProcessIncomingMessage(MSQ.Message message)
        {
            //Save link to Last message
            lastIncomingMessage = message;
            var customer = message.CustomerInfo;
            var behaviour = message.BehaviourFlags;

            // Устанавливаем контекст окна с входящим звонком
            // если окно не существует
            if (incCallWindow == null)
            {
                InvokeGUIThread(() =>
                {
                    // Create Incoming Call Window
                    incCallWindow = new IncomingCallWindow();
                    incCallWindow.Phone = customer.PhoneNumber;
                    incCallWindow.Name = customer.Name;
                    incCallWindow.Address = customer.Addres;
                    incCallWindow.Call = call;
                    if (behaviour.isFinded) // есть данные о клиенте
                        incCallWindow.AddButtonNewOrderAvailable();
                    incCallWindow.Show();
                });
            }
            else // если окно уже существует
            {
                if (behaviour.isFinded) // есть данные о клиенте
                {
                    incCallWindow.SetAttributes(customer.PhoneNumber, customer.Name, customer.Addres);
                    incCallWindow.AddButtonNewOrderAvailable();
                }

            }
        }

        // Получаем сообщения от Orders
        private void OrdersQueue_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e)
        {
            try
            {
                var msg = OrdersQueue.EndReceive(e.AsyncResult);

                BinaryFormatter formatter = new BinaryFormatter();

                if (msg != null)
                {
                    // Десериализую сообщение из потока
                    MSQ.Message message = (MSQ.Message)formatter.Deserialize(msg.BodyStream);
                    // Передаю на обработку в фоновый поток
                    ThreadPool.QueueUserWorkItem(
                        new WaitCallback((object state) =>
                        {
                            ProcessIncomingMessage(message);
                        }), null);
                }
                // Разрешаю прием сообщений
                OrdersQueue.BeginReceive();
            }
            catch (MessageQueueException msqex)
            {
                MessageBox.Show(msqex.Message + Environment.NewLine + msqex.StackTrace);
            }
        }

        /// <summary>
        /// If there is a call in progress it sends a DTMF signal according to the RFC 2833 standard else it makes the dialing number.
        /// </summary>
        private void buttonKeyPadButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null)
                return;

            if (call != null)
                return;

            if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
                txtPhoneNumber.Text = String.Empty;

            txtPhoneNumber.Text += btn.Content.ToString().Trim();
        }

        private void buttonKeyPad_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var btn = sender as Button;

            if (call == null)
                return;

            if (!call.CallState.IsInCall())
                return;

            int dtmfSignal = GetDtmfSignalFromButtonTag(btn);
            if (dtmfSignal == -1)
                return;

            // Звуковой сигнал
            currentDtmfSignal = dtmfSignal;
            call.StartDTMFSignal((DtmfNamedEvents)dtmfSignal);
        }

        private void buttonKeyPad_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (call == null)
                return;

            if (!call.CallState.IsInCall())
                return;

            // Звуковой сигнал
            call.StopDTMFSignal((DtmfNamedEvents)currentDtmfSignal);
        }

        private int GetDtmfSignalFromButtonTag(Button button)
        {
            if (button == null)
                return -1;

            if (button.Tag == null)
                return -1;

            int signal;
            if (int.TryParse(button.Tag.ToString(), out signal))
                return signal;

            return -1;
        }

        private void InitializeSoftphone(object state)
        {
            try
            {
                softPhone = SoftPhoneFactory.CreateSoftPhone(SoftPhoneFactory.GetLocalIP(), 10000, 20000);

                phoneLine = softPhone.CreatePhoneLine(new SIPAccount(true, Login, Login, Login, Password, "192.168.0.5"));

                phoneLine.RegistrationStateChanged += phoneLine_RegistrationStateChanged;

                softPhone.IncomingCall += _softPhone_IncomingCall;

                softPhone.RegisterPhoneLine(phoneLine);

                ConnectMedia();
            }
            catch (Exception ex)
            {
                InvokeGUIThread(() => { MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace); });
            }
        }

        /// <summary>
        /// Connects the microphone and speaker to the call sender and receiver.
        /// </summary>
        private void ConnectMedia()
        {
            if (speaker != null)
                connector.Connect(mediaReceiver, speaker);

            if (microphone != null)
                connector.Connect(microphone, mediaSender);
        }

        // Обработка изменения состояния соединения с сервером
        private void phoneLine_RegistrationStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            if (e.State == RegState.Error || e.State == RegState.NotRegistered)
            {
                InvokeGUIThread(() =>
                {
                    //set unavailable icon and text message
                    var source = new Uri("/SipClient;component/Resources/inactive.png", UriKind.Relative);
                    this.StatusIcon.Source = new BitmapImage(source);
                    txtConnectionStatus.Text = "Нет подключения!";
                });
            }
            else if (e.State == RegState.RegistrationSucceeded) // Online!
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
            return Regex.Match(number, @"^((\+7|7|8)+([0-9]){4})?([0-9]{3})?([0-9]{3})$").Success;
        }

        // Обработка входящего звонка
        private void _softPhone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            // Выводим инфу о звонящем
            var incomingCall = e.Item as IPhoneCall;

            // Отклоняем входящие звонки при наличии активного соединения
            if (call != null)
            {
                incomingCall.Reject();
                return;
            }

            call = incomingCall;
            SubscribeToCallEvents(call);

            //InvokeGUIThread(() =>
            //{
            //    this.txtCallStatus.Text = "Входящий";
            //    this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/call-end.png", UriKind.Relative));
            //});

            // Отправляем данные в Orders
            SendMessageToOrders(e.Item.DialInfo);

            // Проверяем возвращение сообщения от Orders
            // Если сообщение идет более 2 секунд - создаем окно входящего звонка
            ThreadPool.QueueUserWorkItem((object state) =>
            {
                Timer timer = new Timer((object state1) =>
                {
                    if (call != null)
                    {
                        if (call.IsIncoming && incCallWindow == null)
                        {
                            InvokeGUIThread(() =>
                            {
                                // Create new incoming window
                                incCallWindow = new IncomingCallWindow();
                                incCallWindow.Phone = e.Item.DialInfo.CallerID;
                                incCallWindow.Name = e.Item.DialInfo.CallerDisplay;
                                incCallWindow.Call = call;
                                incCallWindow.Show();
                            });
                        }
                    }
                }, null, 1000, 0);
            });
        }

        /// <summary>
        /// Send message to Orders
        /// </summary>
        /// <param name="dialInfo">if dialInfo == null, send old message with specified parameters</param>
        public static void SendMessageToOrders(DialInfo dialInfo)
        {
            // Формируем сообщение и отправляем его
            using (var msg = new System.Messaging.Message())
            {
                //create message
                MSQ.Message msq_message = null;
                // use latest message
                if (dialInfo == null && lastIncomingMessage != null)
                {
                    // Set new behaviour
                    lastIncomingMessage.BehaviourFlags.isCreateNewOrder = CreateNewOrderFlag;
                    // set link to message
                    msq_message = lastIncomingMessage;
                }
                else //or create new message
                {
                    msq_message = new MSQ.Message();
                    msq_message.BehaviourFlags = new MSQ.Behaviour() { isFinded = false, isCreateNewOrder = false };
                    msq_message.CustomerInfo = new MSQ.Customer() { Addres = String.Empty, Name = String.Empty, PhoneNumber = dialInfo.CallerID };
                }
                // Create queue message with guaranteed delivery
                msg.Body = msq_message;
                msg.Recoverable = true;
                msg.Formatter = new BinaryMessageFormatter();
                // Send to sipClient queue
                SipClientQueue.Send(msg);
            }
        }

        // Вызов события изменения состояния звонка
        private void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            InvokeGUIThread(() =>
            {
                txtCallStatus.Text = e.State.ToString();
            });

            if (e.State == CallState.Answered) // звонок принят
            {
                if (microphone != null)
                    microphone.Start();

                if (speaker != null)
                    speaker.Start();

                mediaSender.AttachToCall(call);
                mediaReceiver.AttachToCall(call);
                
                InvokeGUIThread(() =>
                {
                    this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/call-end.png", UriKind.Relative));
                });
            }

            if (e.State.IsCallEnded()) // звонок окончен
            {
                if (microphone != null)
                    microphone.Stop();

                if (speaker != null)
                    speaker.Stop();

                mediaSender.Detach();
                mediaReceiver.Detach();

                UnsubscribeFromCallEvents(sender as IPhoneCall);
                call = null;

                InvokeGUIThread(() =>
                {
                    txtPhoneNumber.Text = String.Empty;
                    txtCallStatus.Text = "Готов";
                    this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/telephone.png", UriKind.Relative));
                });

                //Закрываем окно с входящим звонком
                InvokeGUIThread(() =>
                {
                    if (incCallWindow != null && incCallWindow.IsEnabled)
                    {
                        incCallWindow.Close();

                        incCallWindow = null;
                    };
                });
            }
        }

        /// <summary>
        /// Unsubscribes from the necessary events of a call transact.
        /// </summary>
        private void UnsubscribeFromCallEvents(IPhoneCall call)
        {
            if (call == null)
                return;

            call.CallStateChanged -= (call_CallStateChanged);
            call.DtmfReceived -= (call_DtmfReceived);
        }

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
        /// The controls of the Windows Form applications can only be modified on the GUI thread. This method grants access to the GUI thread.
        /// </summary>
        private void InvokeGUIThread(Action action)
        {
            Dispatcher.Invoke(action, null);
        }


        /// <summary>
        /// Subscribes to the necessary events of a call transact.
        /// </summary>
        private void SubscribeToCallEvents(IPhoneCall call)
        {
            if (call == null)
                return;

            call.CallStateChanged += (call_CallStateChanged);
            call.DtmfReceived += (call_DtmfReceived);
        }

        /// <summary>
        /// Display DTMF signals.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void call_DtmfReceived(object sender, VoIPEventArgs<DtmfInfo> e)
        {
            DtmfSignal signal = e.Item.Signal;
            InvokeGUIThread(() => txtCallStatus.Text = String.Format("DTMF : {0} ", signal.Signal));
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
            //// accept incoming call
            //if (call != null && call.IsIncoming)
            //{
            //    if (call.IsIncoming && call.CallState.IsRinging())
            //    {
            //        call.Reject();
            //    }
            //    else
            //    {
            //        call.HangUp();
            //    }

            //    call = null;
            //    return;
            //}

            // Если введеный номер является номер перевода -> переводим на указанный номер
            if (isTransferNumber("#223"))
            {
                TransferTo("#223");
                return;
            }
         
            // Вызов номера
            CallTo(txtPhoneNumber.Text);
        }

        /// <summary>
        /// Вызов номера по телефону
        /// </summary>
        /// <param name="phoneNumber"></param>
        private void CallTo(string phoneNumber)
        {
            // Если уже есть активное соединение
            if (call != null)
                return;

            // Если соединение не активно!
            if (phoneLine.RegState != RegState.RegistrationSucceeded)
            {
                InvokeGUIThread(() =>
                {
                    MessageBox.Show("Клиент оффлайн. Звонок не возможен!");
                });
                return;
            }

            // Check phone number 
            if (!IsPhoneNumber(phoneNumber) || String.IsNullOrEmpty(phoneNumber))
            {
                MessageBox.Show(string.Concat("Неправильный телефонный номер : '", phoneNumber, "'!"));
                return;
            };

            // make a call to number
            call = softPhone.CreateCallObject(phoneLine, phoneNumber);
            SubscribeToCallEvents(call);
            call.Start();
        }

        private void btn_Hold_Click(object sender, EventArgs e)
        {
            if (call != null)
                call.ToggleHold();
        }

        // изменение ползунка с громкостью
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // change graphics
            if (volumeSlider.Value == 0)
            {
                InvokeGUIThread(() =>
                {
                    var source = new Uri(@"/SipClient;component/Resources/speaker_off_64x64.png", UriKind.Relative);
                    this.SoundIcon.Source = new BitmapImage(source);
                });
            }
            else if (volumeSlider.Value > 0)
            {
                InvokeGUIThread(() =>
                {
                    var source = new Uri(@"/SipClient;component/Resources/speaker_on_64x64.png", UriKind.Relative);
                    this.SoundIcon.Source = new BitmapImage(source);
                });
            }
            speaker.Volume = (float)volumeSlider.Value;
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
            DisconnectMedia();
            softPhone.Close();
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
            DisconnectMedia();
            App.Current.Shutdown();
            SipClientQueue.Close();
            OrdersQueue.Close();
        }

        /// <summary>
        /// Disconnects the microphone and speaker from the call sender and receiver.
        /// </summary>
        private void DisconnectMedia()
        {
            if (speaker != null)
                connector.Disconnect(mediaReceiver, speaker);

            if (microphone != null)
                connector.Disconnect(microphone, mediaSender);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        public string Login { get; set; }

        public string Password { get; set; }

        // Open Call Statistic
        private void miUsername_Click(object sender, RoutedEventArgs e)
        {
            //CallStat callStatistic = new CallStat();
            //callStatistic.Show();
        }

        // Open Login Form
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            //Login loginForm = new Login();
            //loginForm.Show();
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
            if (call == null)
                return;

            if (string.IsNullOrEmpty(transferNumber))
                return;

            if (call.CallState != CallState.InCall)
                return;

            call.BlindTransfer(transferNumber);

            InvokeGUIThread(() =>
            {
                txtCallStatus.Text = "Трансфер на :" + transferNumber;
            });
        }
    }
}
