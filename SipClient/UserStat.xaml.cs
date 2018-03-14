using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data;
using System.Threading.Tasks;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for UserStat.xaml
    /// </summary>
    public partial class UserStat : Window
    {
        public UserStat()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                DataTable tab = Classes.Records.GetDataTable("select * from calls");
                ProcessTable(tab);
            });    
        }

        /// <summary>
        /// The controls of the Windows Form applications can only be modified on the GUI thread. This method grants access to the GUI thread.
        /// </summary>
        private void InvokeGUIThread(Action action)
        {
            Dispatcher.Invoke(action, null);
        }

        private void ProcessTable(DataTable tab)
        {
            if (tab == null)
            {
                // Load empty table
                InvokeGUIThread(() =>
                {
                    MessageBox.Show("Can't load calls.db");
                });
                return;
            }

            // process calls table
            InvokeGUIThread(() =>
            {
                dgvCalls.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = tab });
            });
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnMinimizeClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void btnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
