using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    public partial class Extract : Window
    {
        public Extract(Archiver.MainWindow.Archive archive)
        {
            InitializeComponent();

            this.btnSelectDest.Click += (s, e) => {
                FolderSelector selector = new FolderSelector();
                if(selector.ShowDialog() ?? false) {
                    this.comboBoxDest.Items.Add(selector.SelectedDirectory);
                    this.comboBoxDest.IsEnabled = true;
                    this.comboBoxDest.SelectedIndex = this.comboBoxDest.Items.Count - 1;
                } 
            };

            this.radioExtractAll.Checked += (s, e) => {
                this.panelExclusion.Visibility = Visibility.Collapsed;
            };

            this.radioExclude.Checked += (s, e) => {
                this.panelExclusion.Visibility = Visibility.Visible;
                this.btnSelectExclusion.IsEnabled = true;
            };

            this.btnSelectExclusion.Click += (s, e) => {
                ArchiveWildcardSelect selector = new ArchiveWildcardSelect(archive);
                selector.ShowDialog();
                if (selector.DialogResult == true) {
                    this.comboExclue.Items.Clear();
                    this.comboExclue.Items.Add(selector.SelectedEntries.ToString());
                    this.comboExclue.SelectedIndex = 0;

                    excludedItems = selector.SelectedEntries;
                }
            };

            this.radioNoPass.Checked += (s, e) => {
                this.panelPass.Visibility = Visibility.Collapsed;
            };

            this.radioPass.Checked += (s, e) => {
                this.panelPass.Visibility = Visibility.Visible;
            };

            this.btnCancel.Click += (s, e) => {
                this.Close();
            };

            this.btnOK.Click += (s, e) => {

            };
        }

        FileEntryCollection excludedItems = new FileEntryCollection();

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
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
                return;
            }
            this.WindowState = WindowState.Minimized;
        }
    }
}
