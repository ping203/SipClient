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
        public App()
        {
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
        }

         void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Process unhandled exception
            MessageBox.Show(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
            // Prevent default unhandled exception processing
            e.Handled = true;
        }

         private void Application_Startup(object sender, StartupEventArgs e)
         {
            try
            {
                View.MainWindow window = new View.MainWindow();
                window.Show();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message + Environment.NewLine + exc.StackTrace);
            }            
         }
    }
}
