using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading.Tasks;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for IncomingCallWindow.xaml
    /// </summary>
    public partial class IncomingCallWindow : Window
    {
        public sipdotnet.Call Call { get; set; }

        public sipdotnet.Phone SoftPhone { get; set; }

        public IncomingCallWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void InvokeGUIThread(Action action)
        {
            Dispatcher.Invoke(action, null);
        }

        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Enable echo cancellation
            SoftPhone.GetMediaHandler.EchoCancellation(Call, true);

            // Load Information from mysql
            Task.Factory.StartNew(() =>
            {
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

                    InvokeGUIThread(() => { SetAttributes(Phone, caller_name, address); });
                }
                else
                {
                    InvokeGUIThread(() => { SetAttributes(Phone, Name, Address); });
                }
            });

            //InvokeGUIThread(() => { SetAttributes(Phone, Name, Address); });
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            // принимаем звонок 
            SoftPhone.ReceiveOrResumeCall(this.Call);
        }

        private void btnHoldOn_Click(object sender, RoutedEventArgs e)
        {
            // Если удерживаем звонок
            if (Call != null)
            {
                SoftPhone.HoldCall(this.Call);
            }
        }

        private void btnReject_Click(object sender, RoutedEventArgs e)
        {
            // отклоняем звонок 
            if (Call != null)
            {
                SoftPhone.TerminateCall(this.Call);
            }
            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.Call != null && this.Call.GetState() != sipdotnet.Call.CallState.None)
                SoftPhone.TerminateCall(this.Call);
        }

        internal void SetAttributes(string phone, string name, string address)
        {
            this.txtNameAndPhone.Text = string.Format("Имя : {0}   Телефон : {1}", name, phone);
            this.txtAddress.Text = address;
        }
    }
}
