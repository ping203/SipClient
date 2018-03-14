using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Data.Common;
using System.Data;
namespace SipClient.Classes
{
    public class CallRecord
    {
        private string phone;
        private string time_start;
        private string time_end;
        private int type = 0;

        public string Phone
        {
            get { return phone; }
            set
            {
                phone = value;
            }
        }

        public string TimeStart
        {
            get { return time_start; }
            set
            {
                time_start = value;
            }
        }

        public string TimeEnd
        {
            get { return time_end; }
            set
            {
                time_end = value;
            }
        }

        public int isIncoming
        {
            get { return type; }
            set
            {
                type = value;
            }
        }
    }
}
