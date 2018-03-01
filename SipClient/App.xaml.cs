using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace SipClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
         void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Process unhandled exception
            MessageBox.Show(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
            // Prevent default unhandled exception processing
            e.Handled = true;
        }
    }
}
