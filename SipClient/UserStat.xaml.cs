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

            DataTable tab = Classes.Records.GetDataTable("select * from calls");
            ProcessTable(tab);

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
                    this.Close();
                });
                return;
            }

            // Create special functions
            Func<DataRow, String, DateTime> getCallTime = (DataRow row, String name) =>
            {
                var s = Convert.ToString(row[name]);
                return (!String.IsNullOrEmpty(s)) ? Convert.ToDateTime(s) : new DateTime();
            };

            // processing parallel
            int id = 0;
            var source = (from row in tab.AsEnumerable().AsParallel()
                          select new DisplayedCell()
                          {
                              Id = ++id,
                              Phone = row["Phone"].ToString(),
                              StatusImage = (Convert.ToInt32(row["isIncoming"]) == 1) ?
                                 new BitmapImage(new Uri("/SipClient;component/Resources/inc_call.png", UriKind.Relative))
                                 : new BitmapImage(new Uri("/SipClient;component/Resources/out_call.png", UriKind.Relative)),
                              CallStart = getCallTime(row, "TimeStart"),
                              CallEnd = getCallTime(row, "TimeEnd"),
                          }).OrderBy(elem => elem.Id).ToList(); ;

            // set dgvCalls source
            InvokeGUIThread(() =>
            {
                dgvCalls.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = source });
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

        // inherieted class, define cell of DataGrid
        private class DisplayedCell
        {
            public int Id { get; set; }
            public string Phone { get; set; }
            public BitmapImage StatusImage { get; set; }
            public DateTime CallStart { get; set; }
            public DateTime CallEnd { get; set; }
        }
    }
}
