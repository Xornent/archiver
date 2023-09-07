using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// <summary>
    /// Interaction logic for Extract.xaml
    /// </summary>
    public partial class Rename : Window
    {
        public Rename(string original)
        {
            InitializeComponent();

            this.lbl.Text = $"Rename {original} with:";
            this.txtPass.Text = original;

            this.btnCancel.Click += (s, e) => {
                this.DialogResult = false;
                this.Close();
            };

            this.btnOK.Click += (s, e) => {
                this.DialogResult= true;

                if (this.txtPass.Text.Contains(":") ||
                    this.txtPass.Text.Contains("*") ||
                    this.txtPass.Text.Contains("?") ||
                    this.txtPass.Text.Contains("\"") ||
                    this.txtPass.Text.Contains("<") ||
                    this.txtPass.Text.Contains(">") ||
                    this.txtPass.Text.Contains("|") ) {
                    MessageBox.Show("The name contains invalid character for Windows: \n" +
                        ":, *, ?, \", <, >, | is not permitted.");
                    return;
                }

                this.NewName = this.txtPass.Text;
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
            this.DialogResult = false;
            this.Close();
        }

        private void bdMinimize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
                return;
            }
            this.WindowState = WindowState.Minimized;
        }

        public string NewName { get; set; }
    }
}
