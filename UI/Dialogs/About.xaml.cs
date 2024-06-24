using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Archiver
{
    /// <summary>
    /// Interaction logic for Extract.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

            this.btn7Z.Click += (s, e) => {
                Process.Start(new ProcessStartInfo("https://www.7-zip.org/") { UseShellExecute = true });
            };

            this.btnGithub.Click += (s, e) => {
                Process.Start(new ProcessStartInfo("https://github.com/Xornent/archiver") { UseShellExecute = true });
            };

            this.btnIssue.Click += (s, e) => {
                Process.Start(new ProcessStartInfo("https://github.com/Xornent/archiver/issues") { UseShellExecute = true });
            };

            this.btnClose.Click += (s, e) => {
                this.Close();
            };
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void bdClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void bdMinimize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                return;
            }
            this.WindowState = WindowState.Minimized;
        }
    }
}