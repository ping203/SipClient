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
using System.Diagnostics;
using System.Drawing;
using System.Windows.Interop;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for UserStat.xaml
    /// </summary>
    public partial class UserStat : Window
    {
        private static UserStat instance;
                 
        public static UserStat GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UserStat();
                }
                return instance;
            }
        }

        private UserStat()
        {
            InitializeComponent();
        }

        public void ReloadTable()
        {
            DataTable tab = Classes.Records.GetDataTable("select * from calls");
            ProcessTable(tab);
        }

        private void ProcessTable(DataTable tab)
        {
            if (tab == null)
            {
                // Load empty table
                MessageBox.Show("Can't load calls.db");
                this.Hide();
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
                              id = ++id,
                              phone = row["Phone"].ToString(),
                              bitmap = (Convert.ToInt32(row["isIncoming"]) == 1) ? 
                                Properties.Resources.inc_call : Properties.Resources.out_call ,
                              callStart = getCallTime(row, "TimeStart") ,
                              callEnd = getCallTime(row, "TimeEnd") ,
                          }).OrderBy(elem => elem.id);

            this.dgvCalls.ItemsSource = source;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnMinimizeClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void btnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void dgvCalls_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                MessageBox.Show("left presed!");
            }
        }

        // inherieted class, define cell of DataGrid
        private class DisplayedCell
        {
            public int id { get; set; }
            public string phone { get; set; }
            public Bitmap bitmap { get; set; }
            public DateTime callStart { get; set; }
            public DateTime callEnd { get; set; }

            public ImageSource img
            {
                get
                {
                    return Imaging.CreateBitmapSourceFromHBitmap(
                        bitmap.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            }

        }
    }
}
