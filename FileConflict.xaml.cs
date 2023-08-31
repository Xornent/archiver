using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Archiver
{
    public partial class FileConflict : Window
    {
        public FileConflict(string conflict)
        {
            InitializeComponent();

            string[] lines = conflict.Replace("\r", "").Split('\n');
            this.oPath.Text = lines[2].Substring(4).Parse7zUnicode();
            this.oSize.Text = lines[3].Substring(4);
            this.oModif.Text = lines[4].Substring(4);
            this.nPath.Text = lines[7].Substring(4).Parse7zUnicode();
            this.nSize.Text = lines[8].Substring(4);
            this.nModif.Text = lines[9].Substring(4);

            this.btnA.Click += (s, e) => { this.Response = "A"; this.DialogResult = true; this.Close(); };
            this.btnY.Click += (s, e) => { this.Response = "Y"; this.DialogResult = true; this.Close(); };
            this.btnN.Click += (s, e) => { this.Response = "N"; this.DialogResult = true; this.Close(); };
            this.btnS.Click += (s, e) => { this.Response = "S"; this.DialogResult = true; this.Close(); };
            this.btnU.Click += (s, e) => { this.Response = "U"; this.DialogResult = true; this.Close(); };
            this.btnQ.Click += (s, e) => { this.Response = "Q"; this.DialogResult = true; this.Close(); };
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void bdClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            // do not call 'close' here, because the window if closed without specifying
            // an option will show repeatedly until one option is selected.
            this.Hide();
        }

        private void bdMinimize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
                return;
            }
            this.WindowState = WindowState.Minimized;
        }

        public string Response { get; set; }
    }
}
