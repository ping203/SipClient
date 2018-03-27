using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using sipdotnet;
using System.Drawing;
using System.Windows.Interop;

namespace SipClient.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static Phone Softphone { get; private set; }
        private Account account;
        private Call call;

        public static string Login { get; set; }
        public static string Host { get; set; }
        public static string Password { get; set; }

        public int ProcessID { get; set; }

        public static WcfConnectionService.ServiceClient WcfClient;

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

        private View.IncomingDialog dialog;

        private static Func<string, bool> IsPhoneNumber =
            (string number) => Regex.Match(number, @"^((\+7|7|8)?([0-9]){4})?([0-9]{3})?([0-9]{3})$").Success;

        private string GetCallId(string sip)
        {
            string answer = String.Empty;
            if (sip != null)
            {
                var splitSip = sip.Split(' ');
                if (splitSip.Length > 1)
                {
                    answer = Regex.Match(splitSip[0], @"[^\\""](.+)[^\\""]").Value;
                }
            }
            return answer;
        }

        private string GetPhone(string sip)
        {
            string answer = String.Empty;
            if (sip != null)
            {
                var splitSip = sip.Split(' ');
                if (splitSip.Length > 1)
                {
                    answer = Regex.Match(splitSip[1], @"sip:(\d+)@").Groups[1].Value;
                }
                else
                {
                    answer = Regex.Match(sip, @"sip:(\d+)@").Groups[1].Value;
                }
            }
            return answer;
        }

        /// <summary>
        /// return true if s is transfer phone number
        /// </summary>
        Func<string, bool> isTransferNumber = (s) => Regex.Match(s, @"^(#){1}").Success;

        public MainWindow()
        {
            // Initialize all controls
            InitializeComponent();

            //Load configs
            View.Settings.LoadSettings(View.Settings.PathToConfigs);

            this.SpeakerOff = false;
            this.MicrophoneOff = false;

            this.txtPhoneNumber.KeyDown += new KeyEventHandler(txtPhoneNumber_KeyDown);
        }

        private void InitializeWcfClient()
        {
            WcfClient = new WcfConnectionService.ServiceClient();
            try
            {
                WcfClient.CreateConnection();
                //WcfClient.CallRequestRecieve += new WcfConnectionService.ServiceClient.CallRequestRecieveHandler(WcfClient_CallRequestRecieve);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
            }
        }

        // Recive request from server to call phone number
        //void WcfClient_CallRequestRecieve(string phone)
        //{
        //    if (phone == String.Empty)
        //        return;
        //    // if we have active connection
        //    // ignore this call
        //    if (this.call != null)
        //    {
        //        Task.Factory.StartNew(() =>
        //        {

        //            RecordToLocalDataBase.Phone = phone;
        //            RecordToLocalDataBase.isOutcoming = true;
        //            RecordToLocalDataBase.TimeStart = DateTime.Now;
        //            RecordToLocalDataBase.TimeEnd = DateTime.Now;
        //            Classes.SQLiteBase.AddRecordToDataBase(RecordToLocalDataBase);
        //        });
        //    }
        //    else
        //    {
        //        CallTo(phone);
        //    }
        //}

        private void InitializeSoftphone()
        {
            try
            {
                account = new Account(Login, Password, Host);
                //account.CallerID = "Hello!";
                Softphone = new Phone(account);

                // Add Events
                Softphone.ErrorEvent += new Phone.OnError(softphone_ErrorEvent);
                Softphone.IncomingCallEvent += new Phone.OnIncomingCall(softphone_IncomingCallEvent);
                Softphone.PhoneConnectedEvent += new Phone.OnPhoneConnected(softphone_PhoneConnectedEvent);
                Softphone.PhoneDisconnectedEvent += new Phone.OnPhoneDisconnected(softphone_PhoneDisconnectedEvent);
                Softphone.CallActiveEvent += new Phone.OnCallActive(softphone_CallActiveEvent);
                Softphone.CallCompletedEvent += new Phone.OnCallCompleted(softphone_CallCompletedEvent);

                // Connect to server
                Softphone.Connect();

                // Set Fields
                InvokeGUIThread(() =>
                                {
                                    txtLogin.Text = Login;
                                });
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
                                    {
                                        dialog.Close();
                                        dialog = null;
                                    }
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

            this.call = null;

            InvokeGUIThread(() =>
                            {
                                // Update icons and text
                                txtCallStatus.Text = "Закончен";
                                this.PhoneIcon.Source = ImageSourceFromBitmap(Properties.Resources.telephone);

                                // Disable Timer clockdown
                                //TimerDisable();
                            });

            Classes.LocalAudioPlayer.PlaySound(Properties.Resources.notification_call_ended);
        }

        // Звонок принят или исходящий
        private void softphone_CallActiveEvent(Call call)
        {
            if (isIncoming)
            {
                //Task.Factory.StartNew(() =>
                //                      {
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
                //});
            }
            else
            {
                this.call = call;
                // set outcoming call flag
                isOutcoming = true;

                // Disable echo
                Softphone.GetMediaHandler.EchoCancellation(this.call, Settings.isEchoOff);

                InvokeGUIThread(() =>
                                {
                                    this.txtCallStatus.Text = "Исходящий";
                                    this.PhoneIcon.Source = ImageSourceFromBitmap(Properties.Resources.call_end);

                                    // set new call volume
#warning SetSpeakerSoundDidntWork
                                    //var volValue = this.volumeSlider.Value;
                                    //softphone.GetMediaHandler.SetSpeakerSound(this.call, (float)(volValue % 100));
                                });

                RecordToLocalDataBase.Phone = GetPhone(this.call.GetFrom());
                RecordToLocalDataBase.isOutcoming = true;
                RecordToLocalDataBase.TimeStart = DateTime.Now;
            }
        }

        // Входящий звонок
        private void softphone_IncomingCallEvent(Call incomingCall)
        {
            // Reject incoming call, if we have active
            if (this.call != null)
            {
                Softphone.TerminateCall(incomingCall);

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


            Task.Factory.StartNew(() =>
                                  {
                                      // Disable echo
                                      Softphone.GetMediaHandler.EchoCancellation(this.call, Settings.isEchoOff);

                                      InvokeGUIThread(() =>
                                      {
                                          // change icon to incoming call
                                          this.txtCallStatus.Text = "Входящий";
                                          this.PhoneIcon.Source = ImageSourceFromBitmap(Properties.Resources.call_end);

                                          // show incoming call dialog
                                          dialog = new IncomingDialog();
                                          dialog.Call = this.call;
                                          dialog.SoftPhone = Softphone;
                                          dialog.UpdateTextPanel(GetPhone(this.call.GetFrom()),
                                              GetCallId(this.call.GetFrom()));

                                          dialog.Show();
                                          // Do incoming call panel animation
                                          //AnimationBegin(0.5, brdIncomingCall_Height_Prop_Default, this.brdIncomingCall);
                                      });


                                      RecordToLocalDataBase.Phone = GetPhone(incomingCall.GetFrom());
                                      RecordToLocalDataBase.TimeStart = DateTime.Now;
                                      RecordToLocalDataBase.isRejected = true;

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
                //Debug.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
                Debug.WriteLine(@"WCF соединение не удалось, используем подключение к MySQL бд:
                                    phone : " + phone);

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
            switch (Softphone.CurrentLineState)
            {
                case Phone.LineState.Busy:
                    {
                        // Если введеный номер является номер перевода -> переводим на указанный номер
                        if (isTransferNumber(txtPhoneNumber.Text))
                        {
                            string number = txtPhoneNumber.Text.Remove(0, 1); // remove '#'
                            CallTransferTo(number);
                            return;
                        }
                        // Разрываем активное соединение
                        else if (this.call != null)
                        {
                            Softphone.TerminateCall(call);
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

            Softphone.MakeCall(phoneNumber);


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
            InvokeGUIThread(() =>
                            {
                                // change graphics
                                var volValue = this.volumeSlider.Value;

                                this.volumeSlider.SelectionEnd = volValue;

                                // set new volume
                                if (this.call != null)
                                {
                                    //softphone.GetMediaHandler.SetSpeakerSound(this.call, (float)(volValue / 100));
                                    InvokeGUIThread(() =>
                                    {
#warning SetSoundDidntWork!

                                        //VideoPlayerController.AudioManager.SetApplicationVolume(this.ProcessID, (float)(volValue));
                                    });
                                }
                            });
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

        private void CallTransferTo(string transferNumber)
        {
            // Если введеный номер не правильный или [object]call == nil -> выход
            if (this.call == null || string.IsNullOrEmpty(transferNumber))
                return;

            Softphone.MakeTransfer(this.call, transferNumber);

            InvokeGUIThread(() =>
                            {
                                txtCallStatus.Text = "Трансфер на :" + transferNumber;
                            });

            Classes.LocalAudioPlayer.PlaySound(Properties.Resources.notification_translate);
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

            // Добавим символ с кнокпки
            PutNumberWithDTMF(btn.Content.ToString());
        }

        private void PutNumberWithDTMF(string symb)
        {
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
#warning VolumeDidntOff
            //VideoPlayerController.AudioManager.SetApplicationMute(this.ProcessID, SpeakerOff);
            ChangeIcons();
        }

        private void btnMicOffOn(object sender, RoutedEventArgs e)
        {
            this.MicrophoneOff = !MicrophoneOff;

            Softphone.GetMediaHandler.MicrophoneEnable(!MicrophoneOff);

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

        //private DispatcherTimer timer;
        //private TimeSpan timeSpan;


        //private void AnimationTimerGrow()
        //{
        //    //lblTime.Content = "00:00:00";

        //    //DoubleAnimation animation = new DoubleAnimation();
        //    //animation.From = lblTime.ActualHeight;
        //    //animation.To = 30;
        //    //animation.SpeedRatio = 2;
        //    //animation.Duration = TimeSpan.FromSeconds(1);
        //    //lblTime.BeginAnimation(Label.HeightProperty, animation);
        //}

        //private void AnimationTimerDecrease()
        //{
        //    //DoubleAnimation animation = new DoubleAnimation();
        //    //animation.From = lblTime.ActualHeight;
        //    //animation.To = 0;
        //    //animation.SpeedRatio = 2;
        //    //animation.Duration = TimeSpan.FromSeconds(1);
        //    //lblTime.BeginAnimation(Label.HeightProperty, animation);
        //}

        //internal void StartTimer()
        //{
        //    if (timer == null)
        //        timer = new DispatcherTimer();

        //    timeSpan = new TimeSpan();
        //    timer.Interval = new TimeSpan(0, 0, 0, 0, 0010);
        //    timer.Tick += ((sender, e) =>
        //                   {
        //                       InvokeGUIThread(() =>
        //                                       {
        //                                           timeSpan += timer.Interval;
        //                                           //lblTime.Content = timeSpan.ToString(@"mm\:ss");
        //                                       });
        //                   });
        //    timer.Start();
        //}

        //internal void StopTimer()
        //{
        //    if (timer == null)
        //        return;

        //    timer.Stop();
        //}

        //internal void TimerEnable()
        //{
        //    // load animation
        //    AnimationTimerGrow();
        //    // run timer
        //    StartTimer();
        //}

        //internal void TimerDisable()
        //{
        //    StopTimer();
        //    DispatcherTimer animTimer = new DispatcherTimer();
        //    animTimer.Interval = new TimeSpan(0, 0, 2);
        //    animTimer.Tick += (sender, e) =>
        //    {
        //        AnimationTimerDecrease();
        //        animTimer.Stop();
        //    };
        //    animTimer.Start();
        //}

        // Event hendlers for incoming call
        #region IncomingCallEvents

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            // принимаем звонок
            if (this.call != null)
            {
                Softphone.ReceiveOrResumeCall(this.call);
            }
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            // Если удерживаем звонок
            if (this.call != null)
            {
                Softphone.HoldCall(this.call);

                Classes.LocalAudioPlayer.PlaySound(Properties.Resources.notification_translate);
            }
        }

        private void btnReject_Click(object sender, RoutedEventArgs e)
        {
            // отклоняем звонок
            if (this.call != null)
            {
                Softphone.TerminateCall(this.call);
            }
        }

        #endregion

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

            switch (e.Key)
            {
                // Remove last characters
                case Key.Back:
                case Key.Delete:
                    if (!(txtPhoneNumber.Text.Length == 0))
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
                        string phoneNumber = txtPhoneNumber.Text;
                        CallTo(phoneNumber);
                    }
                    break;
                case Key.Escape:
                    txtPhoneNumber.Text = "";
                    break;
            }
        }

        public bool isIncoming { get; set; }

        public bool isOutcoming { get; set; }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // hold on call
            if (this.call != null)
                Softphone.TerminateCall(this.call);

            // close all conenctions
            Softphone.Disconnect();

            // application shutdown
            App.Current.Shutdown();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                // SoftPhone initialize
                InitializeSoftphone();

                // Create Client connection
                InitializeWcfClient();

                this.ProcessID = Process.GetCurrentProcess().Id;
            });
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new View.Settings();
            if (settings.ShowDialog() == true)
            {
                Task.Factory.StartNew(() =>
                {
                    View.Settings.LoadSettings(View.Settings.PathToConfigs);
                    InitializeSoftphone(); ;
                });
            }
        }

        private void btnShowStatus_Click(object sender, RoutedEventArgs e)
        {
            View.CallList userStat = View.CallList.GetInstance;
            userStat.ReloadTable();
            userStat.Show();
        }
    }
}
