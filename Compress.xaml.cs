using Microsoft.Win32;
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
using Microsoft.WindowsAPICodePack;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

namespace Archiver
{
    /// <summary>
    /// Interaction logic for Extract.xaml
    /// </summary>
    public partial class Compress : Window
    {
        public Compress()
        {
            InitializeComponent();

            this.selectSource.Click += (s, e) => {
                FileSelector selector = new FileSelector();
                selector.ShowDialog();
                if (selector.DialogResult == true) {
                    this.comboBoxSource.Items.Clear();
                    this.comboBoxSource.Items.Add(selector.SelectedEntries.ToString());
                    this.comboBoxSource.SelectedIndex = 0;

                    source = selector.SelectedEntries;

                    sourceParentalDirectory = selector.ParentalDirectory;

                    this.comboBoxDest.Items.Add(selector.ParentalDirectory);
                    dest = selector.ParentalDirectory;
                    this.comboBoxDest.IsEnabled = true;
                    this.comboBoxDest.SelectedIndex = this.comboBoxDest.Items.Count - 1;
                    this.btnOK.IsEnabled = true;
                }
            };

            this.selectWildMask.Click += (s, e) => {
                SystemWildcardSelect selector = new SystemWildcardSelect();
                selector.ShowDialog();
                if (selector.DialogResult == true) {
                    this.comboBoxSource.Items.Clear();
                    this.comboBoxSource.Items.Add(selector.SelectedEntries.ToString());
                    this.comboBoxSource.SelectedIndex = 0;

                    source = selector.SelectedEntries;

                    sourceParentalDirectory = selector.ParentalDirectory;

                    this.comboBoxDest.Items.Add(selector.ParentalDirectory);
                    dest = selector.ParentalDirectory;
                    this.comboBoxDest.IsEnabled = true;
                    this.comboBoxDest.SelectedIndex = this.comboBoxDest.Items.Count - 1;
                    this.btnOK.IsEnabled = true;
                }
            };

            this.selectDest.Click += (s, e) => {
                FolderSelector selector = new FolderSelector();
                selector.ShowDialog();
                if (selector.DialogResult == true) {
                    this.comboBoxDest.Items.Add(selector.SelectedDirectory);
                    dest = selector.SelectedDirectory;
                    this.comboBoxDest.IsEnabled = true;
                    this.comboBoxSource.SelectedIndex = this.comboBoxDest.Items.Count - 1;
                }
            };

            this.comboArchiveType.SelectionChanged += (s, e) => {
                bool flag = false;
                switch ((comboArchiveType.SelectedItem as ComboBoxItem).Content.ToString().ToLower()) {
                    case "zip":
                        flag = true;
                        break;
                    case "bzip2":
                        flag = true;
                        break;
                    case "gzip":
                        flag = true;
                        break;
                    case "xz":
                        break;
                    case "7z":
                        flag = true;
                        break;
                    case "cab":
                        break;
                    case "tar":
                        break;
                }

                this.btnCompressionSettings.IsEnabled = flag;
            };

            this.btnCompressionSettings.Click += (s, e) => {
                CompressionSettingWindow window = null;
                switch((comboArchiveType.SelectedItem as ComboBoxItem).Content.ToString().ToLower()) {
                    case "zip":
                        window = new ZipCompressionSettings(); 
                        break;
                    case "bzip2":
                        window = new BZip2CompressionSettings();
                        break;
                    case "gzip":
                        window = new GZipCompressionSettings();
                        break;
                    case "xz":
                        break;
                    case "7z":
                        window = new SevenZipCompressionSettings();
                        break;
                    case "cab":
                        break;
                    case "tar":
                        break;
                }

                if (window == null) { }
                else {
                    window.ShowDialog();
                    this.methodArgs = window.OptionString;
                }
            };

            this.btnOK.Click += (s, e) => {
                this.isSPF = chkSPF.IsChecked ?? false;
                string archiveName = txtArchiveName.Text.Trim();
                string archiveType = (comboArchiveType.SelectedItem as ComboBoxItem).Content.ToString();

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (wsender, wargs) => {
                    string startup = System.Windows.Forms.Application.StartupPath + @"\";
                    if(!Directory.Exists(startup + @"temp"))
                        Directory.CreateDirectory(startup + @"temp");
                    if (!Directory.Exists(startup + @"working"))
                        Directory.CreateDirectory(startup + @"working");

                    Guid taskGUID = Guid.NewGuid();
                    var tempDir = Directory.CreateDirectory(startup + @"temp\" + taskGUID);
                    var workingDir = Directory.CreateDirectory(startup + @"working\" + taskGUID);
                    string param = $"a {archiveName}.{archiveType} " + 
                                   $"-o\"{dest.FullName}\"";
                    string zpath = startup + @"\7z\x64\7za.exe";

                    if (isSPF) {

                        // generate list file containing all the absolute paths to the
                        // compressed contents.

                        using(FileStream listfile = new FileStream(workingDir.FullName + @"\listfile.txt", FileMode.OpenOrCreate)) {
                            using(StreamWriter writer = new StreamWriter(listfile)) {
                                foreach (var item in source) {
                                    writer.WriteLine(item.FileSystemPath);
                                }

                                writer.Flush();
                            }
                        }

                        param += " -spf @\"" + workingDir.FullName + "\\listfile.txt\""; 

                        Process proc = new Process();
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.CreateNoWindow = true;
                        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        proc.StartInfo.FileName = "cmd.exe";
                        proc.StartInfo.Arguments = zpath + " " + param;
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.StartInfo.RedirectStandardInput = true;
                        proc.StartInfo.RedirectStandardError = true;
                        proc.EnableRaisingEvents = true;

                        proc.Start();

                        bool isOver = false;
                        while (!isOver) {
                            string line = proc.StandardOutput.ReadLine();
                            if (line == null) { isOver = true; continue; }

                            MessageBox.Show(line);
                        }
                    } else {

                    }
                };

                worker.RunWorkerAsync();
            };

            this.btnCancel.Click += (s, e) => {
                this.DialogResult = false;
                this.Close();
            };
        }

        private FileEntryCollection source = null;
        private DirectoryInfo sourceParentalDirectory = null;
        private DirectoryInfo dest = null;
        private string methodArgs = "";
        private bool isSPF = false;

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

    public class CompressionSettingWindow : Window
    {
        public string OptionString { get; set; }
    }
}
