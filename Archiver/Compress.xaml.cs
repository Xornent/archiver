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
using static Archiver.MainWindow;
using Microsoft.VisualBasic.FileIO;

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
                    this.comboBoxDest.SelectedIndex = this.comboBoxDest.Items.Count - 1;
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
                switch ((comboArchiveType.SelectedItem as ComboBoxItem).Content.ToString().ToLower()) {
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

                if (window == null) { } else {
                    window.ShowDialog();
                    this.methodArgs = window.OptionString;
                }
            };

            this.btnOK.Click += (s, e) => {
                this.isSPF = chkSPF.IsChecked ?? false;
                string archiveName = txtArchiveName.Text.Trim();
                string archiveType = (comboArchiveType.SelectedItem as ComboBoxItem).Content.ToString().ToLower();

                string startup = System.Windows.Forms.Application.StartupPath + @"\";
                if (!Directory.Exists(startup + @"temp"))
                    Directory.CreateDirectory(startup + @"temp");
                if (!Directory.Exists(startup + @"working"))
                    Directory.CreateDirectory(startup + @"working");

                Guid taskGUID = Guid.NewGuid();
                var tempDir = Directory.CreateDirectory(startup + @"temp\" + taskGUID);
                var workingDir = Directory.CreateDirectory(startup + @"working\" + taskGUID);
                string param = $"a \"{dest.FullName}\\{archiveName}.{archiveType}\"";

                if(this.radioWithPassword.IsChecked ?? false) {
                    if(this.txtPassword.Text.Contains(" ")) {
                        MessageBox.Show("The password contains invalid characters. Generation cancelled.");
                        return;
                    }

                    param += $" -p{this.txtPassword.Text}";
                }

                if (this.chkRestoreNTSecurity.IsChecked ?? false)
                    param += " -sni";
                if (this.chkIncludeNTFS.IsChecked ?? false)
                    param += " -sns";
                if (this.chkCompressShared.IsChecked ?? false)
                    param += " -ssw";
                if (this.chkSetTimestamp.IsChecked ?? false)
                    param += " -stl";

                if (isSPF) {

                    // generate list file containing all the absolute paths to the
                    // compressed contents.

                    using (FileStream listfile = new FileStream(workingDir.FullName + @"\listfile.txt", FileMode.OpenOrCreate)) {
                        using (StreamWriter writer = new StreamWriter(listfile, Encoding.UTF8)) {
                            foreach (var item in source) {
                                writer.WriteLine(item.FileSystemPath);
                            }

                            writer.Flush();
                        }
                    }

                    param += " -spf -scsUTF-8 @\"" + workingDir.FullName + "\\listfile.txt\"";

                    ActionProcess proc = new ActionProcess(param,
                    $"Compressing {archiveName}.{archiveType} ...",
                    $"Compressing archive file with command line options '{param}'"
                    );

                    proc.Run();

                } else {

                    // copying files

                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (sworker, eworker) => {
                        int id = 0;
                        int full = source.Count;
                        foreach (var item in source) {
                            System.IO.FileInfo fi = new FileInfo(workingDir.FullName + "\\" + item.ArchivePath);
                            System.IO.Directory.CreateDirectory(fi.Directory.FullName);
                            File.Copy(item.FileSystemPath, workingDir.FullName + "\\" + item.ArchivePath);
                            worker.ReportProgress(
                                Convert.ToInt32(100 * (float)id / full),
                                ("Copying files", "Copying file to temporary archive destination ...",
                                 item.ArchivePath)
                                );
                        }
                    };

                    Loader loader = new Loader(worker, true);
                    loader.ShowDialog();

                    param += $" \"{workingDir.FullName}\\*\" -r";
                    ActionProcess proc = new ActionProcess(param,
                    $"Compressing {archiveName}.{archiveType} ...",
                    $"Compressing archive file with command line options '{param}'"
                    );

                    proc.Run();
                }

                if (Directory.Exists(startup + @"temp\" + taskGUID))
                    Directory.Delete(startup + @"temp\" + taskGUID, true);
                if (Directory.Exists(startup + @"working\" + taskGUID))
                    Directory.Delete(startup + @"working\" + taskGUID, true);

                this.Close();
            };

            this.btnCancel.Click += (s, e) => {
                this.Close();
            };

            this.radioWithPassword.Checked += (s, e) => {
                this.panelPassword.Visibility = Visibility.Visible;
            };

            this.radioNoPassword.Checked += (s, e) => {
                this.panelPassword.Visibility = Visibility.Collapsed;
            };
        }

        public Compress(bool fromContextMenu):this()
        {
            List<FileInfo> files = new List<FileInfo>();
            List<DirectoryInfo> directories = new List<DirectoryInfo>();

            if(File.Exists(System.Windows.Forms.Application.StartupPath + @"\" + "context-dirs")) {
                using(FileStream fs = new FileStream(System.Windows.Forms.Application.StartupPath + @"\" + "context-dirs", FileMode.Open)) {
                    using(StreamReader sr = new StreamReader(fs)) {
                        string end = sr.ReadToEnd().Replace("\r","");
                        foreach(string line in end.Split('\n')) {
                            if (string.IsNullOrEmpty(line)) continue;
                            if (Directory.Exists(line))
                                directories.Add(new DirectoryInfo(line));
                            else if (File.Exists(line))
                                files.Add(new FileInfo(line));
                        }
                    }
                }
            }

            FileEntryCollection coll = new FileEntryCollection(files, directories);
            DirectoryInfo parental = new DirectoryInfo(SpecialDirectories.MyDocuments);
            if (files.Any()) { parental = files[0].Directory; }
            else if (directories.Any()) {
                if (directories[0].Parent == null)
                    parental = directories[0];
                else
                    parental = directories[0].Parent;
            }
            this.comboBoxSource.Items.Clear();
            this.comboBoxSource.Items.Add(coll.ToString());
            this.comboBoxSource.SelectedIndex = 0;

            source = coll;

            sourceParentalDirectory = parental;

            this.comboBoxDest.Items.Add(parental);
            dest = parental;
            this.comboBoxDest.IsEnabled = true;
            this.comboBoxDest.SelectedIndex = this.comboBoxDest.Items.Count - 1;
            this.btnOK.IsEnabled = true;
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
