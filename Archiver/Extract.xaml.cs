using System;
using System.Collections.Generic;
using System.IO;
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

                string destination = new FileInfo(archive.FullName).Directory.FullName;
                if (comboBoxDest.SelectedIndex != 0)
                    destination = (comboBoxDest.SelectedItem as DirectoryInfo).FullName;

                string arguments = $"x \"{archive.FullName}\"";

                // exclude working directory
                Guid taskGUID = Guid.NewGuid();
                string startup = System.Windows.Forms.Application.StartupPath + @"\";
                var workingDir = Directory.CreateDirectory(startup + @"working\" + taskGUID);

                if (radioExclude.IsChecked ?? false) {
                    using (FileStream listfile = new FileStream(workingDir.FullName + @"\excludes.txt",
                           FileMode.OpenOrCreate)) {
                        using (StreamWriter writer = new StreamWriter(listfile, Encoding.UTF8)) {
                            foreach (var item in excludedItems) {
                                writer.WriteLine(item.ArchivePath);
                            }

                            writer.Flush();
                        }
                    }
                    arguments += $" -x@\"{workingDir.FullName + @"\excludes.txt"}\"";
                }

                if(radioPass.IsChecked ?? false) {
                    if (!txtPass.Text.Contains(" "))
                        arguments += $" -p{txtPass.Text}";
                    else MessageBox.Show("The password contains invalid characters, and is omitted.");
                }

                string type = "";
                switch (this.comboTypenames.SelectedIndex) {
                    case 0: type = "*"; break;
                    case 1: type = "#"; break;
                    default: type = this.comboTypenames.SelectedItem.ToString().ToLower(); break;
                }
                
                if (this.chkUpperLimit.IsChecked ?? false) {
                    type += ":s";
                    type += this.txtUpperLimit.Text.Trim();
                    switch (this.comboUpperLimitUnit.SelectedIndex) {
                        case 0: type += "b"; break;
                        case 1: type += "k"; break;
                        case 2: type += "m"; break;
                        case 3: type += "g"; break;
                    }
                }

                if (this.chkTypeR.IsChecked ?? false) {
                    type += ":r";
                }

                if (this.chkTypeE.IsChecked ?? false) {
                    type += ":e";
                }

                if (this.chkTypeA.IsChecked ?? false) {
                    type += ":a";
                }

                arguments += $" -t{type}";

                if (this.chkRecurse.IsChecked ?? false) {
                    arguments += " -r";
                } else arguments += " -r-";

                if (this.chkSNI.IsChecked ?? false)
                    arguments += " -sni";
                if (this.chkSNS.IsChecked ?? false)
                    arguments += " -sns";
                switch (this.comboAO.SelectedIndex) {
                    case 0: arguments += " -aoa"; break;
                    case 1: arguments += " -aos"; break;
                    case 2: arguments += " -aou"; break;
                    case 3: arguments += " -aot"; break;
                }

                if (this.chkSPF.IsChecked ?? false) {
                    ExtractSpf spf = new ExtractSpf();
                    if (spf.ShowDialog() ?? false) {
                        arguments += " -spf";
                    } else {
                        this.chkSPF.IsChecked = false;
                        return;
                    }
                } else {
                    arguments += $" -o\"{destination}\"";
                }

                // MessageBox.Show(arguments);

                // Construct arguments
                ActionProcess proc = new ActionProcess(
                    arguments,
                    $"Decompressing {archive.Name} ...",
                    $"Decompressing archive file with command line options '{arguments}'"
                    );

                proc.Run();

                if (Directory.Exists(startup + @"working\" + taskGUID))
                    Directory.Delete(startup + @"working\" + taskGUID, true);
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
