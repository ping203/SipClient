using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Data.Common;
using System.Data;
using System;
namespace SipClient.Classes
{
    public class CallRecord
    {
        private string phone;
        private DateTime time_start;
        private DateTime time_end;
        private bool incoming = false;
        private bool outcoming = false;
        private bool rejected = false;

        public string Phone
        {
            get { return phone; }
            set
            {
                phone = value;
            }
        }

        public DateTime TimeStart
        {
            get { return time_start; }
            set
            {
                time_start = value;
            }
        }

        public DateTime TimeEnd
        {
            get { return time_end; }
            set
            {
                time_end = value;
            }
        }

        public bool isIncoming
        {
            get { return incoming; }
            set
            {
                incoming = value;
            }
        }

        public bool isOutcoming
        {
            get { return outcoming; }
            set
            {
                outcoming = value;
            }
        }

        public bool isRejected
        {
            get { return rejected; }
            set
            {
                rejected = value;
            }
        }
    }
}
