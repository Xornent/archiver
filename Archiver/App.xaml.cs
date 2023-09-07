using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Archiver
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Application.Current.DispatcherUnhandledException -= AppOnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomainUnhandledException;

            Application.Current.DispatcherUnhandledException += AppOnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            // ResourceDictionary dict = new ResourceDictionary();
            // dict.MergedDictionaries.Add(
            //     new ResourceDictionary() { Source = new Uri("UI/Styles.xaml") });
            // Application.Current.Resources = dict;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow window = new MainWindow();
            window.Show();
        }

        public void Activate()
        {
            this.MainWindow.Activate();
        }

        /// <summary>
        /// UI thread unhandled exception.
        /// </summary>
        private static void AppOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
#if DEBUG
#else
            string message = "An unhandled exception is caught in the UI thread.\n\n" +

                "When you see this disclaimer, the program has failed in recovering from exception. " +
                "This exception should be reported to the developers as application BUGS. You can either " +
                "send an e-mail to xornent@outlook.com (Z. Yang) containing the error text (it is automatically " +
                "copied to the clipboard by now), and additionally, detailed description before you " +
                "trigger the error. Or create an issue (recommended practice) at https://github.com/xornent/archiver.\n\n" +

                "Error message: " + e.Exception.Message + "\n\n" +
                "Error stack trace: \n" + e.Exception.StackTrace;

            Exception ex = e.Exception.InnerException;
            while(ex != null) {
                message += "\n\nError message: " + ex.Message + "\n\n" +
                "Error stack trace: \n" + ex.StackTrace;
                ex = ex.InnerException;
            }

            Clipboard.SetText(message);
            MessageBox.Show(message,
                "Unhandled exception",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            try {
                foreach (var taskGUID in Archiver.MainWindow.delayedWorkingDir) {
                    string startup = System.Windows.Forms.Application.StartupPath + @"\";
                    if (Directory.Exists(startup + @"working\" + taskGUID))
                        Directory.Delete(startup + @"working\" + taskGUID, true);
                }
            } catch { }
            e.Handled = true;
            Application.Current.Shutdown();
#endif
        }

        /// <summary>
        /// non-UI thread unhandled exception
        /// </summary>
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if DEBUG
#else
            string message = "An unhandled exception is caught in the non-UI thread.\n\n" +

                "When you see this disclaimer, the program has failed in recovering from exception. " +
                "This exception should be reported to the developers as application BUGS. You can either " +
                "send an e-mail to xornent@outlook.com (Z. Yang) containing the error text (it is automatically " +
                "copied to the clipboard by now), and additionally, detailed description before you " +
                "trigger the error. Or create an issue (recommended practice) at https://github.com/xornent/archiver.\n\n" +

                "Error message: " + ((Exception) e.ExceptionObject).Message + "\n\n" +
                "Error stack trace: \n" + ((Exception)e.ExceptionObject).StackTrace;

            Exception ex = ((Exception)e.ExceptionObject).InnerException;
            while (ex != null) {
                message += "\n\nError message: " + ex.Message + "\n\n" +
                "Error stack trace: \n" + ex.StackTrace;
                ex = ex.InnerException;
            }

            Clipboard.SetText(message);
            MessageBox.Show(message,
                "Unhandled exception",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            try {
                foreach (var taskGUID in Archiver.MainWindow.delayedWorkingDir) {
                    string startup = System.Windows.Forms.Application.StartupPath + @"\";
                    if (Directory.Exists(startup + @"working\" + taskGUID))
                        Directory.Delete(startup + @"working\" + taskGUID, true);
                }
            } catch { }
            Application.Current.Shutdown();
#endif
        }
    }
}
