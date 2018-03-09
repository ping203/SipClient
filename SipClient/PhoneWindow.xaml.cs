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
using System.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using sipdotnet;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PhoneWindow : Window
    {
        private Phone softphone;
        private Account account;
        private Call call;

        private const string _PHONE_NUMBER_HELP = "Введите номер";

        // Message Queue
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
            // Инициализиурем подключение к телефону
            ThreadPool.QueueUserWorkItem(InitializeSoftphone);
            // Инициализиурем подключение к очереди сообщений
            ThreadPool.QueueUserWorkItem((state) =>
            {
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
            });
            // Set Fields
            miUsername.Header = Login;
            volumeSlider.Value = 1f; // set max volume as def
        }

        private static IncomingCallWindow incCallWindow = null;
        private static MSQ.Message lastIncomingMessage;

        // Обрабатываем входящее сообщение в парраллельном потоке
        private void ProcessIncomingMessage(MSQ.Message message)
        {
            //Save link to Last message
            lastIncomingMessage = message;
            var customer = message.CustomerInfo;
            var behaviour = message.BehaviourFlags;

            // Устанавливаем контекст окна с входящим звонком
            if (incCallWindow != null && behaviour.isFinded)
            {
                InvokeGUIThread(() =>
                {
                    incCallWindow.SetAttributes(customer.PhoneNumber, customer.Name, customer.Addres);
                    incCallWindow.AddButtonNewOrderAvailable();
                });
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

        private void InitializeSoftphone(object state)
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

                //Connect to server
                softphone.Connect();
            }
            catch (Exception ex)
            {
                InvokeGUIThread(() => { MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace); });
            }
        }

        private void softphone_CallCompletedEvent(Call call)
        {
            // make null current call
            this.call = null;

            // Close incoming call window
            InvokeGUIThread(() =>
                {
                    if (incCallWindow != null)
                        incCallWindow.Close();
                    txtPhoneNumber.Text = _PHONE_NUMBER_HELP;
                    txtCallStatus.Text = "Закончен";
                    this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/telephone.png", UriKind.Relative));
                });
        }

        private void softphone_CallActiveEvent(Call call)
        {
            InvokeGUIThread(() =>
            {
                this.txtCallStatus.Text = "Входящий";
                this.PhoneIcon.Source = new BitmapImage(new Uri("/SipClient;component/Resources/call-end.png", UriKind.Relative));
            });
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
            // Reject incoming call, if we have actieve
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
                    InvokeGUIThread(() =>
                    {
                        // Create new incoming window
                        incCallWindow = new IncomingCallWindow();
                        incCallWindow.Phone = GetPhone(this.call.GetFrom());
                        incCallWindow.Name = GetCallId(this.call.GetFrom());
                        incCallWindow.Call = this.call;
                        incCallWindow.SoftPhone = this.softphone;
                        incCallWindow.Show();
                    });
                }
            }
        }

        private Func<string, string> GetCallId = (string sip) => Regex.Match(sip, @"[\\""](\w+)[\\""]").Groups[1].Value;
        private Func<string, string> GetPhone = (string sip) => Regex.Match(sip, @"sip:(\d+)@").Groups[1].Value;

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

        // Check number
        private bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"^((\+7|7|8)+([0-9]){4})?([0-9]{3})?([0-9]{3})$").Success;
        }

        /// <summary>
        /// Send message to Orders
        /// </summary>
        public static void SendMessageToOrders()
        {
            //Формируем сообщение и отправляем его
            using (var msg = new System.Messaging.Message())
            {
                //create message
                MSQ.Message msq_message = null;
                //use latest message
                if (lastIncomingMessage != null)
                {
                    //Set new behaviour
                    lastIncomingMessage.BehaviourFlags.isCreateNewOrder = CreateNewOrderFlag;
                    //set link to message
                    msq_message = lastIncomingMessage;
                }
                else //or create new message
                {
                    string phone = string.Empty;
                    if (incCallWindow != null)
                        phone = incCallWindow.Phone;

                    msq_message = new MSQ.Message();
                    msq_message.BehaviourFlags = new MSQ.Behaviour() { isFinded = false, isCreateNewOrder = false };
                    msq_message.CustomerInfo = new MSQ.Customer() { Addres = String.Empty, Name = String.Empty, PhoneNumber = phone };
                }
                //Create queue message with guaranteed delivery
                msg.Body = msq_message;
                msg.Recoverable = true;
                msg.Formatter = new BinaryMessageFormatter();
                //Send to sipClient queue
                SipClientQueue.Send(msg);
            }
        }

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
            //speaker.Volume = (float)volumeSlider.Value;
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
        public string Host { get; set; }
        public string Password { get; set; }

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

        private void micSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void btnVolumeOffOn(object sender, RoutedEventArgs e)
        {

        }

        private void btnMicOffOn(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // close all conenctions
            this.softphone.Disconnect();
            OrdersQueue.ReceiveCompleted -= new ReceiveCompletedEventHandler(OrdersQueue_ReceiveCompleted);
            SipClientQueue.Dispose();
            OrdersQueue.Dispose();

            // application shutdown
            App.Current.Shutdown();
        }
    }
}
