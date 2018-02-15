using System;
using System.Linq;
using System.Text;
using System.Windows;
using Ozeki.VoIP;
using System.ComponentModel;
using Ozeki.Media;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // add log system
        private StringBuilder LogMessages = new StringBuilder(100);

        private static Softphone _softphone; // softphone object

        public MainWindow()
        {
            // Initialize all controls
            InitializeComponent();

            // Initialize sofphone components
            InitSoftphone();

            ReadRegisterInfos();
        }

        /// <summary>
        /// Initializes the softphone logic and subscribes to its events to get notifications from it.
        /// (eg. the registration state of the phone line has changed or an incoming call received)
        /// </summary>
        private void InitSoftphone()
        {
            _softphone = new Softphone();
            // add event handlers
            _softphone.PhoneLineStateChanged += _softphone_RegStateChanged;  // соединение с asterisk
            _softphone.CallStateChanged += _softphone_CallStateChanged;     // статус соединения с клиентом
        }

        /// <summary>
        /// This will be called when the state of the call has changed. (eg. ringing, answered, rejected)
        /// </summary>
        private void _softphone_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            Console.WriteLine("Call state changed: {0}", e.State);

            if (e.State.IsCallEnded())
            {
                StartToDial();
            }
        }

        /// <summary>
        /// This will be called when the registration state of the phone line has changed.
        /// </summary>
        private void _softphone_RegStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            AddToLogMessage(string.Format("Phone line state changed: {0}", e.State));

            if (e.State == RegState.Error || e.State == RegState.NotRegistered)
            {
                ReadRegisterInfos();
            }
            else if (e.State == RegState.RegistrationSucceeded)
            {
                AddToLogMessage("Registration succeeded - ONLINE");
                StartExample();
                StartToDial();
            }
        }

        private void AddToLogMessage(string text)
        {
            LogMessages.Append(string.Concat(text, Environment.NewLine));
        }

        // Reads a number from the user as string, and than makes a call by using the softphone's StartCall() method.
        private void StartToDial()
        {
            AddToLogMessage("To start a call, type the number and press Enter: ");
            string numberToDial = "89648468742";
            while (string.IsNullOrEmpty(numberToDial))
            {
                numberToDial = Console.ReadLine();
            }
            _softphone.StartCall(numberToDial);
        }

        private void StartExample()
        {
            bool inputOK = true;
            while (inputOK)
            {
                AddToLogMessage("\nPlease select the codecs you would like to use by providing their numbers separated with commas (or if you wish to use the default codes, please press Enter): ");

                string codec = Read("Codecs", false);

                if (string.IsNullOrEmpty(codec))
                {
                    AddToLogMessage("Default settings.");
                    WriteEnabledCodecs();
                    inputOK = false;
                }
                else
                {
                    var codecs = codec.Split(',');

                    foreach (var s in _softphone.Codecs())
                    {
                        // This line disables all of the default codecs that are used.
                        _softphone.DisableCodec(s.PayloadType);
                    }

                    foreach (var s in codecs)
                    {
                        try
                        {
                            var codecPayload = Convert.ToInt32(s);

                            if (_softphone.Codecs().
                                Any(item => item.PayloadType == codecPayload))
                            {
                                _softphone.EnableCodec(codecPayload);
                                inputOK = false;
                            }
                            else
                            {
                                AddToLogMessage(string.Format("Invalid payload type: {0}", codecPayload));
                            }

                        }
                        catch (Exception)
                        {
                            AddToLogMessage(string.Format("Invalid payload type: {0}", s));
                        }

                    }
                    if (inputOK == false)
                    {
                        WriteEnabledCodecs();
                    }
                }
            }
        }


        /// <summary>
        /// A helper method for reading the inputs. Even handles, if an information is necessary.
        /// </summary>
        private string Read(string inputName, bool readWhileEmpty)
        {
            while (true)
            {
                string input = Console.ReadLine();

                if (!readWhileEmpty)
                    return input;

                if (!string.IsNullOrEmpty(input))
                    return input;

                AddToLogMessage(inputName + " cannot be empty.");
                AddToLogMessage(inputName + ": ");
            }
        }

        /// <summary>
        /// This method will display the names of the enabled codecs on the consol.
        /// </summary>
        private void WriteEnabledCodecs()
        {
            AddToLogMessage("\nEnabled codecs:");
            foreach (var codecInfo in _softphone.Codecs())
            {
                if (codecInfo.Enabled)
                {
                    AddToLogMessage(string.Format("{0,3} {1}", codecInfo.PayloadType, codecInfo.CodecName));
                }
            }
        }

        private void ReadRegisterInfos()
        {
            AddToLogMessage("-----------------------------------------------");
            AddToLogMessage("List of the available codecs:");
            WriteCodecs();
        }

        /// <summary>
        /// This method write to the consol the names of the available codecs.
        /// </summary>
        private void WriteCodecs()
        {

            AddToLogMessage("\nAudio codecs:");
            foreach (var codecInfo in _softphone.Codecs())
            {
                if (codecInfo.MediaType == CodecMediaType.Audio)
                {
                    AddToLogMessage(string.Format("{0,3} {1}", codecInfo.PayloadType, codecInfo.CodecName));
                }
            }

            AddToLogMessage("\nVideo codecs:");
            foreach (var codecInfo in _softphone.Codecs())
            {
                if (codecInfo.MediaType == CodecMediaType.Video)
                {
                    AddToLogMessage(string.Format("{0,3} {1}", codecInfo.PayloadType, codecInfo.CodecName));
                }
            }
        }

        // On the Connect button Clicked
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
