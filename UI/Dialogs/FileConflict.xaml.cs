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
        public FileConflict(string existingPath, string existingSize, string existingModified,
            string archivePath, string archiveSize, string archiveModified)
        {
            InitializeComponent();

            this.oPath.Text = existingPath;
            this.oSize.Text = existingSize;
            this.oModif.Text = existingModified;
            this.nPath.Text = archivePath;
            this.nSize.Text = archiveSize;
            this.nModif.Text = archiveModified;

            this.btnA.Click += (s, e) => { this.Response = "A"; this.DialogResult = true; this.Close(); };
            this.btnY.Click += (s, e) => { this.Response = "Y"; this.DialogResult = true; this.Close(); };
            this.btnN.Click += (s, e) => { this.Response = "N"; this.DialogResult = true; this.Close(); };
            this.btnS.Click += (s, e) => { this.Response = "S"; this.DialogResult = true; this.Close(); };
            this.btnU.Click += (s, e) => { this.Response = "U"; this.DialogResult = true; this.Close(); };
            this.btnQ.Click += (s, e) => { this.Response = "Q"; this.DialogResult = true; this.Close(); };
        }

        private void titleBarDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void windowClose(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void windowStateChange(object sender, MouseButtonEventArgs e)
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
