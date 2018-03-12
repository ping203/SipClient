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
        private string time;
        private int type;

        public string Phone
        {
            get { return phone; }
            set
            {
                phone = value;
            }
        }

        public string Time
        {
            get { return time; }
            set
            {
                time = value;
            }
        }

        public int Type
        {
            get { return type; }
            set
            {
                type = value;
            }
        }
    }
}
