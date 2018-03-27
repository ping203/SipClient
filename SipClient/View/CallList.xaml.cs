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
using MahApps.Metro.Controls;
using System.Windows.Interop;
using System.Drawing;
using System.Data;

namespace SipClient.View
{
    /// <summary>
    /// Interaction logic for CallList.xaml
    /// </summary>
    public partial class CallList : MetroWindow
    {
        private static CallList instance;

        public static CallList GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CallList();
                }
                return instance;
            }
        }

        private CallList()
        {
            InitializeComponent();
        }

        public void ReloadTable()
        {
            DataTable tab = Classes.SQLiteBase.GetDataTable("select * from calls");

            this.dgvCalls.ItemsSource = ProcessTable(tab);
        }

        public List<LastPhones> GetLastPhoneNumbersWithIcon
        {
            get
            {
                List<LastPhones> answer = null;
                var dt = new DataTable();
                try
                {
                    dt = Classes.SQLiteBase.GetDataTable(@"select distinct(Phone),isIncoming,isOutcoming,isRejected
                                                                from calls where Phone != '' order by TimeStart;");
                }
                catch (Exception)
                {
                }
                if (dt != null && dt.Rows.Count > 0)
                {
                    answer = (from row in dt.AsEnumerable()
                              select new LastPhones()
                              {
                                  phone = row["Phone"].ToString(),
                                  bitmap = (Convert.ToInt32(row["isIncoming"]) == 1) ? Properties.Resources.inc_call
                                            : (Convert.ToInt32(row["isOutcoming"]) == 1) ? Properties.Resources.out_call
                                            : (Convert.ToInt32(row["isRejected"]) == 1) ? Properties.Resources.rej_call
                                            : Properties.Resources.close
                              }).ToList();
                }
                return answer;
            }
        }

        public class LastPhones
        {
            public string phone { get; set; }
            public Bitmap bitmap { get; set; }

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

        private List<DisplayedCell> ProcessTable(DataTable tab)
        {
            if (tab == null || tab.Rows.Count == 0)
            {
                return new List<DisplayedCell>();
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
                              bitmap = (Convert.ToInt32(row["isIncoming"]) == 1) ? Properties.Resources.inc_call
                                        : (Convert.ToInt32(row["isOutcoming"]) == 1) ? Properties.Resources.out_call
                                        : (Convert.ToInt32(row["isRejected"]) == 1) ? Properties.Resources.rej_call
                                        : Properties.Resources.close
                              ,
                              callStart = getCallTime(row, "TimeStart"),
                              callEnd = getCallTime(row, "TimeEnd"),
                          }).OrderBy(elem => elem.id).ToList();

            return source;
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
        class DisplayedCell
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

        private void btnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Classes.SQLiteBase.RemoveAllRecords();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message + Environment.NewLine + exc.StackTrace);
            }
        }
    }
}
