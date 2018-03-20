using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace SipClientTesting
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CheckIsNumber()
        {
            var flag1 = SipClient.PhoneWindow.IsPhoneNumber("9003138242");
            Assert.IsTrue(flag1);

            var flag2 = SipClient.PhoneWindow.IsPhoneNumber("256");
            Assert.IsTrue(flag2);

            var flag3 = SipClient.PhoneWindow.IsPhoneNumber("+79648468742");
            Assert.IsTrue(flag3);

            var flag4 = SipClient.PhoneWindow.IsPhoneNumber("89648468742");
            Assert.IsTrue(flag4);

            var flag5 = SipClient.PhoneWindow.IsPhoneNumber("510031");
            Assert.IsTrue(flag5);
        }

      
    }
}
