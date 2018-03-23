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

        private static Func<string, bool> IsPhoneNumber = (string number) => Regex.Match(number, @"^((\+7|7|8)?([0-9]){4})?([0-9]{3})?([0-9]{3})$").Success;

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

            this.txtPhoneNumber.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(txtPhoneNumber_PreviewMouseLeftButtonDown);
            this.txtPhoneNumber.GotFocus += new RoutedEventHandler(txtPhoneNumber_GotFocus);
            this.txtPhoneNumber.LostFocus += new RoutedEventHandler(txtPhoneNumber_LostFocus);
            this.txtPhoneNumber.TextChanged += new TextChangedEventHandler(txtPhoneNumber_TextChanged);
            this.txtPhoneNumber.KeyDown += new KeyEventHandler(txtPhoneNumber_KeyDown);

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

            //InvokeGUIThread(() =>
            //{
            //    //this.brdCallerInfo.Visibility = System.Windows.Visibility.Collapsed;

            //    //brdIncomingCall_Height_Prop_Default = brdIncomingCall.ActualHeight;

            //    //AnimationEnd(0.001, this.brdIncomingCall);
            //});

            Task.Factory.StartNew(() =>
                                  {
                                      //for (int i = 0; i < 20; i++)
                                      //{
                                      //Thread.Sleep(2000);

                                      //InvokeGUIThread(() =>
                                      //{
                                      //    IncomingCallDialog.GetInstance.ShowDialog();
                                      //    //AnimationBegin(1.0, brdIncomingCall_Height_Prop_Default, brdIncomingCall);
                                      //});

                                      //Thread.Sleep(5000);

                                      //InvokeGUIThread(() =>
                                      //{
                                      //    IncomingCallDialog.GetInstance.ShowDialog();
                                      //});

                                      //    Thread.Sleep(2000);

                                      //    InvokeGUIThread(() =>
                                      //    {
                                      //        SetCallerInfoPanelAttributes("89992221111", "", "");
                                      //    });

                                      //    Thread.Sleep(2000);

                                      //    InvokeGUIThread(() =>
                                      //    {
                                      //        AnimationEnd(1.0, brdIncomingCall);
                                      //    });
                                      //}
                                  });
        }

        // Высота формы brdIncomingCall as default
        //private double brdIncomingCall_Height_Prop_Default;

        //private void AnimationBegin(double time, double toSize, Border border)
        //{
        //    //ResetCallerInfoPanelAttributes();

        //    border.IsEnabled = true;
        //    // use animation
        //    DoubleAnimation da = new DoubleAnimation();
        //    da.From = border.ActualHeight;
        //    da.To = toSize;
        //    da.Duration = TimeSpan.FromSeconds(time);

        //    border.BeginAnimation(Border.HeightProperty, da);
        //}

        //private void AnimationEnd(double time, Border border)
        //{
        //    // Hide IncomingCallPanel
        //    border.IsEnabled = false;
        //    // use animation
        //    DoubleAnimation da = new DoubleAnimation();
        //    da.From = border.ActualHeight;
        //    da.To = 0;
        //    da.Duration = TimeSpan.FromSeconds(time);

        //    border.BeginAnimation(Border.HeightProperty, da);
        //}

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
#warning CallerID didn't set!
                //account.CallerID = "Hello!";
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

        // Звонок завершился
        private void softphone_CallCompletedEvent(Call call)
        {
#warning НеобходимыПравки!
            if (isIncoming)
            {
                // write to database
                try
                {
                    RecordToLocalDataBase.isIncoming = true;
                    RecordToLocalDataBase.TimeEnd = DateTime.Now;
                    Classes.SQLiteBase.AddRecordToDataBase(RecordToLocalDataBase);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
                }
                // drop down Incoming call form
                InvokeGUIThread(() =>
                                {
                                    ResetCallerInfoPanelAttributes();

                                    // hide incoming call dialog
                                    if (dialog != null)
                                        dialog.AnimationEnd();
                                    //AnimationEnd(0.5, this.brdIncomingCall);
                                    //remove info panel
                                    //this.brdCallerInfo.Child = null;
                                });
            }
            else if (isOutcoming) // outcoming call
            {
                // write to database
                try
                {
                    RecordToLocalDataBase.isOutcoming = true;
                    RecordToLocalDataBase.TimeEnd = DateTime.Now;
                    Classes.SQLiteBase.AddRecordToDataBase(RecordToLocalDataBase);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
                }
            }
            else // rejected or another type
            {

            }

            // clear flags
            isIncoming = false;
            isOutcoming = false;

            InvokeGUIThread(() =>
                            {
                                // Update icons and text
                                txtPhoneNumber.Text = _PHONE_NUMBER_HELP;
                                txtCallStatus.Text = "Закончен";
                                this.PhoneIcon.Source = ImageSourceFromBitmap(Properties.Resources.telephone);

                                // Disable Timer clockdown
                                //TimerDisable();
                            });

            Classes.LocalAudioPlayer.StopSound();

            this.call = null;
        }

        // Звонок принят или исходящий
        private void softphone_CallActiveEvent(Call call)
        {
            if (isIncoming)
            {
                Task.Factory.StartNew(() =>
                                      {
                                          // sound notification off
                                          Classes.LocalAudioPlayer.StopSound();

                                          // write incoming call to base
                                          try
                                          {
                                              RecordToLocalDataBase.Phone = GetPhone(this.call.GetFrom());
                                              RecordToLocalDataBase.TimeStart = DateTime.Now;
                                              RecordToLocalDataBase.isRejected = false;
                                          }
                                          catch (Exception e)
                                          {
                                              MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
                                          }
                                      });
            }
            else
            {
                this.call = call;
                // set outcoming call flag
                isOutcoming = true;

                InvokeGUIThread(() =>
                                {
                                    this.txtCallStatus.Text = "Входящий";
                                    this.PhoneIcon.Source = ImageSourceFromBitmap(Properties.Resources.call_end);

                                    // set new call volume
#warning SetSpeakerSoundDidntWork
                                    var volValue = this.volumeSlider.Value;
                                    softphone.GetMediaHandler.SetSpeakerSound(this.call, (float)(volValue % 100));
                                });

                Task.Factory.StartNew(() =>
                                      {
                                          //                     Enable timer clockdown
                                          //                     TimerEnable();

                                          // write call start time
                                          try
                                          {
                                              RecordToLocalDataBase.Phone = GetPhone(this.call.GetFrom());
                                              RecordToLocalDataBase.isOutcoming = true;
                                              RecordToLocalDataBase.TimeStart = DateTime.Now;
                                          }
                                          catch (Exception e)
                                          {
                                              MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
                                          }
                                      });
            }
        }

        // Входящий звонок
        private void softphone_IncomingCallEvent(Call incomingCall)
        {
            // Reject incoming call, if we have active
            if (this.call != null)
            {
                softphone.TerminateCall(incomingCall);

                // write rejected call to base
                try
                {
                    RecordToLocalDataBase.Phone = GetPhone(incomingCall.GetFrom());
                    RecordToLocalDataBase.TimeStart = DateTime.Now;
                    RecordToLocalDataBase.isRejected = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
                }

                return;
            };

            // set flag
            isIncoming = true;
            // Recieve incoming call
            this.call = incomingCall;

            InvokeGUIThread(() =>
                            {
                                // change icon to incoming call
                                this.txtCallStatus.Text = "Входящий";
                                this.PhoneIcon.Source = ImageSourceFromBitmap(Properties.Resources.call_end);

                                // show incoming call dialog
                                dialog = new IncomingCallDialog();
                                dialog.Call = this.call;
                                dialog.SoftPhone = softphone;
                                // \"OPERATOR10\" <sip:oper-sauri10@192.168.0.5>
                                dialog.UpdateTextPanel(GetPhone(this.call.GetFrom()), GetCallId(this.call.GetFrom()));
                                dialog.Show();
                                // Do incoming call panel animation
                                //AnimationBegin(0.5, brdIncomingCall_Height_Prop_Default, this.brdIncomingCall);
                            });

            Task.Factory.StartNew(() =>
                                  {
                                      // write rejected call to base
                                      try
                                      {
                                          RecordToLocalDataBase.Phone = GetPhone(incomingCall.GetFrom());
                                          RecordToLocalDataBase.TimeStart = DateTime.Now;
                                          RecordToLocalDataBase.isRejected = true;
                                      }
                                      catch (Exception e)
                                      {
                                          MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
                                      }

                                      // Play Sound
                                      Classes.LocalAudioPlayer.PlaySound(Properties.Resources.signal, true);

                                      // Get information about caller
                                      GetInfoAboutCaller(this.call.GetFrom());
                                  });
        }

        /// <summary>
        /// Get caller info from call.GetInfo()
        /// </summary>
        /// <param name="CallGettFrom"></param>
        public void GetInfoAboutCaller(string CallGettFrom)
        {
            bool tryToUseMySQL = false;
            string phone = GetPhone(CallGettFrom);
            // use wcf connection
            try
            {
                if (this.call != null)
                {
                    var clientInfo = WcfClient.GetClinetInformation(phone);

                    InvokeGUIThread(() =>
                                    {
                                        SetCallerInfoPanelAttributes(clientInfo.Phone, clientInfo.Name, clientInfo.Address);
                                        if (dialog != null)
                                            dialog.UpdateTextPanel(clientInfo.Phone, clientInfo.Name);
                                    });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + Environment.NewLine + e.StackTrace);

                tryToUseMySQL = true;
            }

            // Use Mysql connection
            if (tryToUseMySQL && this.call != null)
            {
                var tabCallerInfo = Classes.MainDataBase.GetDataTable(
                    string.Format(
                        @"select hat.order_id,hat.customer,hat.customer_name as name,addr.address_text
                        from orders_hat hat left join customers_addresses addr on hat.order_id=addr.order_id
                        where hat.customer = '{0}' order by date_sm desc limit 1;", phone));
                // Set new values to caller_name , address
                if (tabCallerInfo != null && tabCallerInfo.Rows.Count > 0)
                {
                    string caller_name = Convert.ToString(tabCallerInfo.Rows[0]["name"]);
                    if (caller_name == "")
                        caller_name = GetCallId(CallGettFrom);

                    string address = Convert.ToString(tabCallerInfo.Rows[0]["address_text"]);

                    InvokeGUIThread(() =>
                                    {
                                        SetCallerInfoPanelAttributes(phone, caller_name, address);
                                        if (dialog != null)
                                            dialog.UpdateTextPanel(phone, caller_name);
                                    });
                }
                else
                {
                    InvokeGUIThread(() =>
                                    {
                                        string callId = GetCallId(CallGettFrom);
                                        SetCallerInfoPanelAttributes(phone, callId, "");
                                        if (dialog != null)
                                            dialog.UpdateTextPanel(phone, callId);
                                    });
                }
            }
        }

        private void ResetCallerInfoPanelAttributes()
        {
            this.lblAddress.Visibility = System.Windows.Visibility.Collapsed;
            this.lblName.Visibility = System.Windows.Visibility.Collapsed;
            this.lblPhone.Visibility = System.Windows.Visibility.Collapsed;

            brdCallerInfo.Visibility = System.Windows.Visibility.Collapsed;
        }

        // Установка инфы о пользователе
        private void SetCallerInfoPanelAttributes(string phone, string name, string address)
        {
            if (this.call == null)
                return;

            if (!String.IsNullOrEmpty(phone))
            {
                lblPhone.Content = "Телефонный номер : " + phone;
                this.lblPhone.Visibility = System.Windows.Visibility.Visible;

                brdCallerInfo.Visibility = System.Windows.Visibility.Visible;
            }
            if (!String.IsNullOrEmpty(name))
            {
                lblName.Content = "Имя : " + name;
                this.lblName.Visibility = System.Windows.Visibility.Visible;

                brdCallerInfo.Visibility = System.Windows.Visibility.Visible;
            }
            if (!String.IsNullOrEmpty(address))
            {
                lblAddress.Content = "Адрес " + address;
                this.lblAddress.Visibility = System.Windows.Visibility.Visible;

                brdCallerInfo.Visibility = System.Windows.Visibility.Visible;
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
                                            this.StatusIcon.Source = ImageSourceFromBitmap(Properties.Resources.inactive);
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

            softphone.MakeCall(phoneNumber);


            //if (!String.IsNullOrEmpty(Login))
            //{
            //    // make call with sip:uri
            //    // sip:user:password@host:port;uri-parameter?headers
            //    //softphone.Useragent = "test";
            //     softphone.MakeCall("227");

            //    //softphone.MakeCall("sip:test:testpass@192.168.0.5:5060;<transport>=<UDP>;<>=<>");

            //    //softphone.MakeCall(string.Format("sip:{0}:{1}@{2};<user>=<{3}>",Login,Password, Host, phoneNumber));
            //}
            //else
            //{
            //    // make a call to number
            //    softphone.MakeCall(phoneNumber);
            //}
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
#warning SetSoundDidntWork!
                softphone.GetMediaHandler.SetSpeakerSound(this.call, (float)(volValue / 100));
            }
        }

        //private void Window_KeyDown(object sender, KeyEventArgs e)
        //{
        //    switch (e.Key)
        //    {
        //        // Remove last characters
        //        case Key.Back:
        //        case Key.Delete:
        //            if (!txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP) && !(txtPhoneNumber.Text.Length == 0))
        //            {
        //                txtPhoneNumber.Text = txtPhoneNumber.Text.Remove(txtPhoneNumber.Text.Length - 1, 1);
        //            }
        //            break;
        //        // short input keys
        //        case Key.NumPad0:
        //        case Key.D0:
        //            {
        //                buttonKeyPadButton_Click(new Button() { Content = 0 }, e);
        //                // PutNumberWithDTMF("0");
        //            }
        //            break;
        //        case Key.NumPad1:
        //        case Key.D1:
        //            {
        //                //put number
        //                buttonKeyPadButton_Click(new Button() { Content = 1 }, e);
        //                //PutNumberWithDTMF("1");
        //            }
        //            break;
        //        case Key.NumPad2:
        //        case Key.D2:
        //            {
        //                //put number
        //                buttonKeyPadButton_Click(new Button() { Content = 2 }, e);
        //                //PutNumberWithDTMF("2");
        //            }
        //            break;
        //        case Key.NumPad3:
        //        case Key.D3:
        //            {
        //                //put number
        //                buttonKeyPadButton_Click(new Button() { Content = 3 }, e);
        //                //PutNumberWithDTMF("3");
        //            }
        //            break;
        //        case Key.NumPad4:
        //        case Key.D4:
        //            {
        //                //put number
        //                buttonKeyPadButton_Click(new Button() { Content = 4 }, e);
        //                //PutNumberWithDTMF("4");
        //            }
        //            break;
        //        case Key.NumPad5:
        //        case Key.D5:
        //            {
        //                //put number
        //                buttonKeyPadButton_Click(new Button() { Content = 5 }, e);
        //                //PutNumberWithDTMF("5");
        //            }
        //            break;
        //        case Key.NumPad6:
        //        case Key.D6:
        //            {
        //                //put number
        //                buttonKeyPadButton_Click(new Button() { Content = 6 }, e);
        //                //PutNumberWithDTMF("6");
        //            }
        //            break;
        //        case Key.NumPad7:
        //        case Key.D7:
        //            {
        //                //put number
        //                buttonKeyPadButton_Click(new Button() { Content = 7 }, e);
        //                //PutNumberWithDTMF("7");
        //            }
        //            break;
        //        case Key.NumPad8:
        //        case Key.D8:
        //            {
        //                //put number
        //                buttonKeyPadButton_Click(new Button() { Content = 8 }, e);
        //                //PutNumberWithDTMF("8");
        //            }
        //            break;
        //        case Key.NumPad9:
        //        case Key.D9:
        //            {
        //                //put number
        //                buttonKeyPadButton_Click(new Button() { Content = 9 }, e);
        //                //PutNumberWithDTMF("9");
        //            }
        //            break;
        //        // calling to number
        //        case Key.Enter:
        //            {
        //                if (!txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
        //                {
        //                    string phoneNumber = txtPhoneNumber.Text;
        //                    CallTo(phoneNumber);
        //                }
        //            }
        //            break;
        //    }
        //}

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

        //private static bool createNewOrderFlag;

        ///// <summary>
        ///// Return flag and unset him
        ///// </summary>
        //public static bool CreateNewOrderFlag
        //{
        //    get
        //    {
        //        bool oldValue = createNewOrderFlag;
        //        createNewOrderFlag = false;
        //        return oldValue;
        //    }
        //    set
        //    {
        //        createNewOrderFlag = value;
        //    }
        //}

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

            InvokeGUIThread(() =>
                            {
                                this.btnAdditionalIcon.Source = PhoneWindow.ImageSourceFromBitmap(Properties.Resources.close);
                            });


            //if (this.txtPhoneNumber.Text == _PHONE_NUMBER_HELP || this.txtPhoneNumber.Text == string.Empty)
            //{
            //    InvokeGUIThread(() =>
            //    {
            //        this.btnAdditionalIcon.Source = PhoneWindow.ImageSourceFromBitmap(Properties.Resources.down);
            //    });
            //}
            //else
            //{
            //    InvokeGUIThread(() =>
            //    {
            //        this.btnAdditionalIcon.Source = PhoneWindow.ImageSourceFromBitmap(Properties.Resources.close);
            //    });
            //}
        }

        private void PutNumberWithDTMF(string symb)
        {
            // Если не подсказка для ввода текста
            if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
            {
                txtPhoneNumber.TextChanged -= new TextChangedEventHandler(txtPhoneNumber_TextChanged);
                txtPhoneNumber.Text = String.Empty;
                txtPhoneNumber.TextChanged += new TextChangedEventHandler(txtPhoneNumber_TextChanged);
            }

            // Play Sound
            Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY[symb]);

            // add to phone text
            txtPhoneNumber.Text += symb;
        }

        public bool SpeakerOff { get; set; }
        public bool MicrophoneOff { get; set; }

        private void btnVolumeOffOn(object sender, RoutedEventArgs e)
        {
            SpeakerOff = !SpeakerOff;

            ChangeIcons();
        }

        private void btnMicOffOn(object sender, RoutedEventArgs e)
        {
            this.MicrophoneOff = !MicrophoneOff;

            softphone.GetMediaHandler.MicrophoneEnable(!MicrophoneOff);

            ChangeIcons();
        }

        private double volumeSliderValue = 0;

        private void ChangeIcons()
        {
            if (!SpeakerOff)
            {
                InvokeGUIThread(() =>
                                {
                                    this.SpeakerIcon.Source = ImageSourceFromBitmap(Properties.Resources.speaker_on_64x64);
                                    this.volumeSlider.Value = volumeSliderValue;
                                });
            }
            else
            {
                InvokeGUIThread(() =>
                                {
                                    volumeSliderValue = this.volumeSlider.Value;
                                    this.SpeakerIcon.Source = ImageSourceFromBitmap(Properties.Resources.speaker_off_64x64);
                                    this.volumeSlider.Value = 0;
                                });
            }

            if (!MicrophoneOff)
            {
                InvokeGUIThread(() => { this.MicIcon.Source = ImageSourceFromBitmap(Properties.Resources.mic_on); });
            }
            else
            {
                InvokeGUIThread(() => { this.MicIcon.Source = ImageSourceFromBitmap(Properties.Resources.mic_off); });
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // close all conenctions
            softphone.Disconnect();

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
        private IncomingCallDialog dialog;

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

        // Event hendlers for incoming call
        #region IncomingCallEvents

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            // принимаем звонок
            if (this.call != null)
            {
                softphone.ReceiveOrResumeCall(this.call);
            }
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            // Если удерживаем звонок
            if (this.call != null)
            {
                softphone.HoldCall(this.call);

                Classes.LocalAudioPlayer.PlaySound(Properties.Resources.notification_translate);
            }
        }

        private void btnReject_Click(object sender, RoutedEventArgs e)
        {
            // отклоняем звонок
            if (this.call != null)
            {
                softphone.TerminateCall(this.call);
            }
        }

        #endregion

        private void Item_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Control)
            {
                Control control = (sender as Control);
                //if (control.Name == "btnClose")
                //    control.Background = System.Windows.Media.Brushes.Purple;
                //else
                control.Background = System.Windows.Media.Brushes.WhiteSmoke;
            }
        }

        private void Item_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Control).Background = null;
        }

        // Дпоплнительный контрол для поля ввода телефонного номера
        private void btnPhoneNumberAdditional_Click(object sender, RoutedEventArgs e)
        {
            txtPhoneNumber.Text = "";

            //            if (txtPhoneNumber.Text == _PHONE_NUMBER_HELP || txtPhoneNumber.Text == "")
            //            {
            //                var list = UserStat.GetInstance.GetLastPhoneNumbersWithIcon;
            //                // выводим список
            //                if (list != null && list.Count > 0)
            //                {
            //#warning WatchLastCalls
            //                    var source = UserStat.GetInstance.GetLastPhoneNumbersWithIcon;
            //                    dgvCallList.ItemsSource = source;
            //                    dgvCallList.Visibility = System.Windows.Visibility.Visible;
            //                }
            //            }
            //            else
            //            {
            //                txtPhoneNumber.Text = "";
            //            }
        }

        #region txtPhoneNumberEvents

        private void txtPhoneNumber_GotFocus(object sender, RoutedEventArgs e)
        {
            //change background color
            this.borderPhoneNumber.BorderBrush = System.Windows.Media.Brushes.Red;
        }

        private void txtPhoneNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            //change background color
            this.borderPhoneNumber.BorderBrush = System.Windows.Media.Brushes.Green;

            if (txtPhoneNumber.Text == "")
            {
                txtPhoneNumber.Text = _PHONE_NUMBER_HELP;
            }
        }

        void txtPhoneNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsRepeat)
            {
                e.Handled = true;
            }
#warning отсекаем ввод букв с клавиатуры
            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                e.Handled = true;
                return;
            }

            if (txtPhoneNumber.Text.Equals(_PHONE_NUMBER_HELP))
            {
                txtPhoneNumber.TextChanged -= new TextChangedEventHandler(txtPhoneNumber_TextChanged);
                txtPhoneNumber.Text = "";
                txtPhoneNumber.TextChanged += new TextChangedEventHandler(txtPhoneNumber_TextChanged);
            }

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
                        // Play Sound
                        Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY["0"]);
                    }
                    break;
                case Key.NumPad1:
                case Key.D1:
                    {
                        // Play Sound
                        Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY["1"]);
                    }
                    break;
                case Key.NumPad2:
                case Key.D2:
                    {
                        // Play Sound
                        Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY["2"]);
                    }
                    break;
                case Key.NumPad3:
                case Key.D3:
                    {
                        // Play Sound
                        Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY["3"]);
                    }
                    break;
                case Key.NumPad4:
                case Key.D4:
                    {
                        // Play Sound
                        Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY["4"]);
                    }
                    break;
                case Key.NumPad5:
                case Key.D5:
                    {
                        // Play Sound
                        Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY["5"]);
                    }
                    break;
                case Key.NumPad6:
                case Key.D6:
                    {
                        // Play Sound
                        Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY["6"]);
                    }
                    break;
                case Key.NumPad7:
                case Key.D7:
                    {
                        // Play Sound
                        Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY["7"]);
                    }
                    break;
                case Key.NumPad8:
                case Key.D8:
                    {
                        // Play Sound
                        Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY["8"]);
                    }
                    break;
                case Key.NumPad9:
                case Key.D9:
                    {
                        // Play Sound
                        Classes.LocalAudioPlayer.PlaySound(Classes.LocalAudioPlayer.DTFMS_DICTONARY["9"]);
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
                case Key.Escape:
                    txtPhoneNumber.Text = "";
                    break;
            }
        }

        void txtPhoneNumber_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (txtPhoneNumber.Text == _PHONE_NUMBER_HELP)
            {
                txtPhoneNumber.TextChanged -= new TextChangedEventHandler(txtPhoneNumber_TextChanged);
                txtPhoneNumber.Text = "";
                txtPhoneNumber.TextChanged += new TextChangedEventHandler(txtPhoneNumber_TextChanged);
            }
        }

        void txtPhoneNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPhoneNumber.Text == "")
            {
                txtPhoneNumber.Text = _PHONE_NUMBER_HELP;
            }
        }


        #endregion

        public bool isIncoming { get; set; }

        public bool isOutcoming { get; set; }

    }
}
