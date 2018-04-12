using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using sipdotnet;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Threading;
using SipClient.Classes;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

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

        //public static WcfConnectionService.ServiceClient WcfClient;

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
        Func<string, bool> isTransferNumber = (s) => Regex.Match(s, @"(#){1}").Success;

        private Classes.ViewModel viewModel
            = new Classes.ViewModel();

        public MainWindow()
        {
            // Initialize all controls
            InitializeComponent();

            this.DataContext = viewModel;

            //Load configs
            View.Settings.LoadSettings(View.Settings.PathToConfigs);

            // Set up current Culture info
            Culture = new System.Globalization.CultureInfo("ru-RU");

            this.SpeakerEnabled = true;
            this.MicrophoneEnabled = true;

            this.txtPhoneNumber.KeyDown += new KeyEventHandler(txtPhoneNumber_KeyDown);
        }

        public bool WcfClientAvailable { get; private set; }

        private void InitializeWcfClient()
        {
            //WcfClient = new WcfConnectionService.ServiceClient();
            //try
            //{
            //    WcfClient.CreateConnection();

            //    // test connection
            //    WcfClient.GetClinetInformation("227");

            //    WcfClientAvailable = true;
            //}
            //catch (Exception e)
            //{
            //    WcfClientAvailable = false;
            //}

            //InvokeGUIThread(() =>
            //{
            //    txtWcfService.Text = (WcfClientAvailable) ? "WCF : Good" : "WCF : No";
            //});
        }

        // Recive request from server to call phone number
        void WcfClient_CallRequestRecieve(string phone)
        {
            if (phone == String.Empty)
                return;
            // if we have active connection
            // ignore this call
            if (this.call != null)
            {
                // record to sqlite db rejected call
                Task.Factory.StartNew(() =>
                {
                    RecordToLocalDataBase.Phone = phone;
                    RecordToLocalDataBase.isOutcoming = true;
                    RecordToLocalDataBase.TimeStart = DateTime.Now;
                    RecordToLocalDataBase.TimeEnd = DateTime.Now;
                    Classes.SQLiteBase.AddRecordToDataBase(RecordToLocalDataBase);
                });
            }
            else
            {
                CallTo(phone);
            }
        }

        private void InitializeSoftphone()
        {
            try
            {
                account = new Account(Settings.Login, Settings.Account,
                     Settings.Password, Settings.Host, Settings.Port);

                Softphone = new Phone(account);

                // Add Events
#warning DisableLogEvent
                //Softphone.LogEvent += Softphone_LogEvent;  //Enable Log System
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
                                    this.txtAccount.Text = Settings.Account;
                                });
            }
            catch (Exception ex)
            {
                InvokeGUIThread(() =>
                {
                    MessageBox.Show("Не удается создать объект : SoftPhone !"
                        + Environment.NewLine + "Сообщение ошибки : " + ex.Message);
                });
            }
        }

        private void Softphone_LogEvent(string message)
        {
            Debug.WriteLine("[DEBUG] " + message);
        }


        #region Sofphone_Events

        private void softphone_PhoneDisconnectedEvent()
        {
            InvokeGUIThread(() =>
                            {
                                //set unavailable icon and text message
                                StatusIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "presenceNotAvailable");
                                txtStatus.Text = "Нет подключения!";
                            });
        }

        private void softphone_PhoneConnectedEvent()
        {
            InvokeGUIThread(() =>
                            {
                                //set available icon and text message
                                PhoneIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "answer");
                                this.btnConnectOrReject.Background = Brushes.Green;
                                StatusIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "presenceAvailable");
                                txtStatus.Text = "Подключен!";
                            });
        }

        // Звонок завершился
        private void softphone_CallCompletedEvent(Call call)
        {
            if (isIncoming)
            {
                // drop down Incoming call form
                InvokeGUIThread(() =>
                                {
                                    // Close borderCallNotification
                                    CloseCallNotification();

                                    // hide incoming call dialog
                                    if (dialog != null)
                                    {
                                        dialog.Close();
                                        dialog = null;
                                    }

                                    // add badge value to history button
                                    int value = string.IsNullOrEmpty(viewModel.BadgeValue) ? 0 : Convert.ToInt32(viewModel.BadgeValue);
                                    viewModel.BadgeValue = (++value).ToString();
                                });
            }

            // write to database
            RecordToLocalDataBase.isOutcoming = isOutcoming;
            RecordToLocalDataBase.isIncoming = isIncoming;
            RecordToLocalDataBase.TimeEnd = DateTime.Now;
            Classes.SQLiteBase.AddRecordToDataBase(RecordToLocalDataBase);

            // clear flags
            isIncoming = false;
            isOutcoming = false;

            this.call = null;

            InvokeGUIThread(() =>
                            {
                                txtStatus.Text = "Свободно";
                                PhoneIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "answer");
                                StatusIcon.SetResourceReference(Image.SourceProperty, "presenceAvailable");
                                btnConnectOrReject.Background = Brushes.Green;

                                CloseCallNotification();

                                // discard blur effect to infoPanel
                                if (borderCallNotification.BitmapEffect != null)
                                {
                                    borderCallNotification.BitmapEffect = null;
                                    borderCallNotification.Background = null;
                                }
                            });

            Classes.LocalAudioPlayer.PlaySound(Properties.Resources.notification_call_ended);

        }

        // Звонок принят или исходящий
        private void softphone_CallActiveEvent(Call call)
        {
            if (isIncoming)
            {
                // sound notification off
                Classes.LocalAudioPlayer.StopSound();

                if (dialog != null)
                    dialog.Close();

                // write incoming call to base
                RecordToLocalDataBase.Phone = GetPhone(call.From);
                RecordToLocalDataBase.TimeStart = DateTime.Now;
                RecordToLocalDataBase.isRejected = false;
            }
            else
            {
                this.call = call;
                // set outcoming call flag
                isOutcoming = true;

                // Disable echo
                Softphone.EchoCancellation(this.call, Settings.isEchoOff);
                string phone = GetPhone(call.To);

                //show caller info
                GetInfoAboutCaller(call.To);

                InvokeGUIThread(() =>
                                {
                                    txtStatus.Text = "Занят";
                                    lblStatusRecord.Text = "Исходящий";
                                    PhoneIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "decline");
                                    StatusIcon.SetResourceReference(Image.SourceProperty, "presence_not_available");
                                    btnConnectOrReject.Background = Brushes.Red;
                                });

                // set volume
                Softphone.SetSpeakerValue(this.call, (float)volumeSliderValue);

                // db record
                RecordToLocalDataBase.Phone = phone;
                RecordToLocalDataBase.isOutcoming = true;
                RecordToLocalDataBase.TimeStart = DateTime.Now;
                //Classes.SQLiteBase.AddRecordToDataBase(RecordToLocalDataBase);
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
                RecordToLocalDataBase.Phone = GetPhone(incomingCall.From);
                RecordToLocalDataBase.TimeStart = DateTime.Now;
                RecordToLocalDataBase.isRejected = true;

                return;
            };

            // set flag
            isIncoming = true;
            // Recieve incoming call
            this.call = incomingCall;

            //Echo cancellation
            Softphone.EchoCancellation(this.call, Settings.isEchoOff);

            string phone = GetPhone(this.call.From);
            string callId = GetCallId(this.call.From);

            InvokeGUIThread(() =>
            {
                dialog = new IncomingDialog();
                // show incoming call dialog      
                dialog.Call = this.call;
                dialog.SoftPhone = Softphone;
                dialog.UpdateTextPanel(phone, callId);

                dialog.Show();

                // change icon to incoming call
                this.txtStatus.Text = "Занят";
                lblStatusRecord.Text = "Входящий";
                PhoneIcon.SetResourceReference(Image.SourceProperty, "decline");
                StatusIcon.SetResourceReference(Image.SourceProperty, "presence_not_available");
                this.btnConnectOrReject.Background = Brushes.Red;
            });

            // Play Sound
#pragma warning disable CS0103 // The name 'Properties' does not exist in the current context
#pragma warning disable CS0103 // The name 'Properties' does not exist in the current context
            Classes.LocalAudioPlayer.PlaySound(Properties.Resources.signal, true);
#pragma warning restore CS0103 // The name 'Properties' does not exist in the current context
#pragma warning restore CS0103 // The name 'Properties' does not exist in the current context

            // record call info
            RecordToLocalDataBase.Phone = phone;
            RecordToLocalDataBase.TimeStart = DateTime.Now;

            // Get information about caller
            GetInfoAboutCaller(call.From);
        }

        /// <summary>
        /// Get caller info from call.GetInfo()
        /// </summary>
        /// <param name="CallGettFrom"></param>
        public void GetInfoAboutCaller(string CallGettFrom)
        {
            string phone = GetPhone(CallGettFrom);
            string callID = GetCallId(CallGettFrom);

            // use wcf connection
            //if (this.call != null && WcfClientAvailable)
            //{
            //    //var clientInfo = WcfClient.GetClinetInformation(phone);

            //    InvokeGUIThread(() =>
            //    {
            //        var name = clientInfo.Name == "" ? callID : clientInfo.Name;

            //        ShowCallNotificationPanel(clientInfo.Phone, name, clientInfo.Address);

            //        if (dialog != null)
            //            dialog.UpdateTextPanel(clientInfo.Phone, name);
            //    });
            //}
            //else // Use Mysql connection
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
                        caller_name = callID;

                    string address = Convert.ToString(tabCallerInfo.Rows[0]["address_text"]);

                    InvokeGUIThread(() =>
                                    {
                                        ShowCallNotificationPanel(phone, caller_name, address);
                                        if (dialog != null)
                                            dialog.UpdateTextPanel(phone, caller_name);
                                    });
                }
                else // 
                {
                    InvokeGUIThread(() =>
                                    {
                                        ShowCallNotificationPanel(phone, callID, "");
                                        if (dialog != null)
                                            dialog.UpdateTextPanel(phone, callID);
                                    });
                }
            }
        }

        private static DoubleAnimation animationCallNotifPanel = new DoubleAnimation();
        private static DispatcherTimer tmrWait = new DispatcherTimer();

        // Установка инфы о пользователе
        private void ShowCallNotificationPanel(string phone, string name, string address)
        {
            if (this.call == null)
                return;

            StartWatchTimer();
            txtCallerPhoneNumber.Text = phone;
            borderCallNotification.Visibility = Visibility.Visible;

            int size = 63;

            if (!String.IsNullOrEmpty(name))
            {
                txtCallerName.Visibility = Visibility.Visible;
                txtCallerName.Text = "Имя : " + name;
                size = 90;
            }

            if (!String.IsNullOrEmpty(address))
            {
                txtCallerAddress.Visibility = Visibility.Visible;
                txtCallerAddress.Text = address;
                size = 140;
            }
            // use double animation to show panel
            animationCallNotifPanel.From = 0;
            animationCallNotifPanel.To = size;
            animationCallNotifPanel.Duration = TimeSpan.FromMilliseconds(600);
            borderCallNotification.BeginAnimation(Border.HeightProperty, animationCallNotifPanel);
        }

        // wait 3 sec and hide borderCallNotification
        private void CloseCallNotification()
        {
            StopWatchimer();

            // wait 2 sec
            tmrWait.Interval = new TimeSpan(0, 0, 2);
            tmrWait.Tick += (s, ex) =>
            {
                // stop timer 
                (s as DispatcherTimer).Stop();
                // hide animation
                animationCallNotifPanel.From = borderCallNotification.ActualHeight;
                animationCallNotifPanel.To = 0;
                animationCallNotifPanel.Duration = TimeSpan.FromMilliseconds(600);
                borderCallNotification.BeginAnimation(Border.HeightProperty, animationCallNotifPanel);

                //clear text fields
                txtCallerName.Text = "";
                txtCallerAddress.Text = "";

                txtCallerName.Visibility = Visibility.Collapsed;
                txtCallerAddress.Visibility = Visibility.Collapsed;
            };
            tmrWait.Start();
        }

        private void softphone_ErrorEvent(Call call, Phone.Error error)
        {
            switch (error)
            {
                case Phone.Error.CallError:
                    InvokeGUIThread(() =>
                                    {
                                        txtStatus.Text = "Ошибка вызова!";
                                    });
                    break;
                case Phone.Error.LineIsBusyError:
                    InvokeGUIThread(() =>
                                    {
                                        txtStatus.Text = "Линия занята!";
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
                                            StatusIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "presenceNotAvailable");
                                            txtStatus.Text = "Нет подключения!";
                                        });
                    }
                    break;
                case Phone.Error.UnknownError:
                    {
                        InvokeGUIThread(() =>
                                        {
                                            txtStatus.Text = "Неизвестная ошибка!";
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
                            string text = txtPhoneNumber.Text;
                            string number = text.Replace("#", "");// remove all '#'
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
        }

        // изменение ползунка с громкостью
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            InvokeGUIThread(() =>
                            {
                                // change graphics
                                volumeSliderValue = this.volumeSlider.Value;

                                this.volumeSlider.SelectionEnd = volumeSliderValue;
                            });

            // set new volume
            if (this.call != null)
            {
                Softphone.SetSpeakerValue(this.call, (float)volumeSliderValue);
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

        private void CallTransferTo(string transferNumber)
        {
            // Если введеный номер не правильный или [object]call == nil -> выход
            if (this.call == null || string.IsNullOrEmpty(transferNumber))
                return;
#warning MakeTransferNatieve
            Softphone.TransferCall(this.call, transferNumber);

            LocalAudioPlayer.PlaySound(Properties.Resources.notification_translate);
        }

        private void buttonKeyPadButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            if (call == null && btn.Content.Equals('#')) return;

            //Вводим номер для перевода
            bool transferFlag = (isTransferNumber(txtPhoneNumber.Text) || btn.Content.Equals("#"));

            if (transferFlag)
            //if (txtPhoneNumber.Text.Contains("#"))
            {
                InvokeGUIThread(() =>
                                {
                                    txtStatus.Text = "Трансфер на..";
                                    PhoneIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "answer");
                                    this.btnConnectOrReject.Background = Brushes.Green;
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

        public bool SpeakerEnabled { get; set; }
        public bool MicrophoneEnabled { get; set; }

        private static double volumeSliderValue;

        private void btnVolumeOffOn(object sender, RoutedEventArgs e)
        {
            SpeakerEnabled = !SpeakerEnabled;

#warning SpeakerDidntDisabled
            // Softphone.SpeakerEnabled = SpeakerOff;

            if (SpeakerEnabled)
            {
                InvokeGUIThread(() =>
                {
                    SpeakerIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "speaker_on");
                    volumeSlider.Value = volumeSliderValue;
                });
            }
            else
            {
                InvokeGUIThread(() =>
                {
                    volumeSliderValue = volumeSlider.Value;
                    SpeakerIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "speaker_off");
                    volumeSlider.Value = 0;
                });
            }
        }

        private void btnMicOffOn(object sender, RoutedEventArgs e)
        {
            this.MicrophoneEnabled = !MicrophoneEnabled;

            Softphone.MicrophoneEnabled = MicrophoneEnabled;

            if (MicrophoneEnabled)
            {
                InvokeGUIThread(() =>
                {
                    MicIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "mic_on");
                });
            }
            else
            {
                InvokeGUIThread(() =>
                {
                    MicIcon.SetResourceReference(System.Windows.Controls.Image.SourceProperty, "mic_off");
                });
            }
        }

        // Event hendlers for incoming call
        #region IncomingCallEvents

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (this.call != null)
            {
                // resume call
                if (this.call.State == Call.CallState.Hold)
                {
                    // discard blur effect to infoPanel
                    if (borderCallNotification.BitmapEffect != null)
                    {
                        borderCallNotification.BitmapEffect = null;
                        borderCallNotification.Background = null;
                    }
                    // resume
                    Softphone.ResumeCall(this.call);

                    // Play notification
                    LocalAudioPlayer.PlaySound(Properties.Resources.incoming_chat);
                }
                else
                    // receive call
                    Softphone.ReceiveCall(this.call);
            }
        }

        private void btnPauseCall_Click(object sender, RoutedEventArgs e)
        {
            // Если удерживаем звонок
            if (this.call != null)
            {
#warning HoldCallDisactieve
                // add call to call stack
                Softphone.HoldCall(this.call);
                //Softphone.HoldCall(this.call);
                Classes.LocalAudioPlayer.PlaySound(Properties.Resources.notification_delayed);
                // apply blur effect to infoPanel
                BlurBitmapEffect effect = new BlurBitmapEffect();
                borderCallNotification.Background = Brushes.LightGray;
                effect.Radius = 4;
                effect.KernelType = KernelType.Gaussian;
                borderCallNotification.BitmapEffect = effect;
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
#pragma warning disable CS1030 // #warning: 'отсекаем ввод букв с клавиатуры'
#warning отсекаем ввод букв с клавиатуры
            if (e.Key >= Key.A && e.Key <= Key.Z)
#pragma warning restore CS1030 // #warning: 'отсекаем ввод букв с клавиатуры'
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

        public CultureInfo Culture { get; private set; }

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
            });

            this.txtCallerName.Visibility = Visibility.Collapsed;
            this.txtCallerAddress.Visibility = Visibility.Collapsed;
            this.borderCallNotification.Visibility = Visibility.Collapsed;

            volumeSliderValue = 1.0;
            this.volumeSlider.Value = volumeSliderValue;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new Settings();
            if (settings.ShowDialog() == true)
            {
                Task.Factory.StartNew(() =>
                {
                    View.Settings.LoadSettings(View.Settings.PathToConfigs);
                InitializeSoftphone(); ;
                });
            }
        }

        private void btnShowNumpad_Click(object sender, RoutedEventArgs e)
        {
            this.panelCallHistory.Visibility = Visibility.Collapsed;
            this.gridNumpad.Visibility = Visibility.Visible;
        }

        private void btnCalls_Click(object sender, RoutedEventArgs e)
        {
            // Load table history
            var source = LoadCallHistory();
            InvokeGUIThread(() =>
            {
                dgvCallHistory.ItemsSource = source;
            });

            gridNumpad.Visibility = Visibility.Collapsed;
            panelCallHistory.Visibility = Visibility.Visible;
        }

        //Fast Call To Number
        private void dgvCallHistory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CallHistoryCell item = dgvCallHistory.SelectedItem as CallHistoryCell;
            if (item != null)
            {
                CallTo(item.phone);
            }
        }

        // show short info window
        private void dgvCallHistory_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CallHistoryCell item = dgvCallHistory.SelectedItem as CallHistoryCell;

        }

        private void txtSearchHistory_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = txtSearchHistory.Text;
            if (string.IsNullOrEmpty(text))
            {
                var source = LoadCallHistory();
                InvokeGUIThread(() => { dgvCallHistory.ItemsSource = source; });
            }
            else
            {
                var source = (dgvCallHistory.ItemsSource as List<CallHistoryCell>);

                if (source.Count == 0)
                    source = LoadCallHistory();
                var finded = source.AsEnumerable()
                    .Where(item => item.phone.StartsWith(text)).ToList();

                InvokeGUIThread(() => { dgvCallHistory.ItemsSource = finded; });
            }
        }

        private List<CallHistoryCell> LoadCallHistory()
        {
            DataTable tab = SQLiteBase.GetDataTable("select * from calls where Phone <> ''");
            return ProcessTable(tab);
        }

        private List<CallHistoryCell> ProcessTable(DataTable tab)
        {
            if (tab == null || tab.Rows.Count == 0)
            {
                return new List<CallHistoryCell>();
            }

            return (from row in tab.AsEnumerable()
                    select new CallHistoryCell()
                    {
                        phone = row["Phone"].ToString()
                        ,
                        bitmap = (Convert.ToInt32(row["isIncoming"]) == 1) ? Properties.Resources.inc_call : Properties.Resources.out_call
                        ,
                        time = Convert.ToDateTime(row["TimeStart"], Culture)
                    }).ToList();
        }

        private static DispatcherTimer timer = new DispatcherTimer();

        internal void StartWatchTimer()
        {
            var timeSpan = new TimeSpan();
            InvokeGUIThread(() =>
            {
                lblTimer.Text = timeSpan.ToString(@"hh\:mm\:ss", Culture);
            });

            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += ((sender, e) =>
                           {
                               InvokeGUIThread(() =>
                                               {
                                                   timeSpan += timer.Interval;
                                                   lblTimer.Text = timeSpan.ToString(@"hh\:mm\:ss", Culture);
                                               });
                           });

            timer.Start();
        }

        internal void StopWatchimer()
        {
            if (timer == null)
                return;

            timer.Stop();
        }

        private void dgvCallHistory_MenuCallToClick(object sender, RoutedEventArgs e)
        {
            CallHistoryCell item = dgvCallHistory.SelectedItem as CallHistoryCell;
            if (item != null)
            {
                txtPhoneNumber.Text = item.phone;
                CallTo(item.phone);
            }
        }

        private void cmbAllorSkiped_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ContentControl)((object[])e.AddedItems)[0]).Content == "Все")
            {
                LoadCallHistory();
            }
            else
            {
                DataTable tab = SQLiteBase.GetDataTable("select * from calls where Phone <> '' and isRejected = 1");
                dgvCallHistory.ItemsSource = ProcessTable(tab);
            }
        }
    }
}
