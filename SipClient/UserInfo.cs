using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SipClient
{
    class UserInfo
    {
        public UserInfo(string userName, string realName, string country, string note)
        {
            UserName = userName;
            RealName = realName;
            Country = country;
            Note = note;
        }

        public string UserName { get; private set; }
        public string RealName { get; private set; }
        public string Country { get; private set; }
        public string Note { get; private set; }
    }
}
