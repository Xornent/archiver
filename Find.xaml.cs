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
    public partial class Find : Window
    {
        public Find(Archiver.MainWindow.Archive archive, bool isSPF)
        {
            InitializeComponent();
            if (isSPF) {
                this.btnCut.IsEnabled = false;
                this.btnDelete.IsEnabled = false;
            }

            // visit all available items.
            void visitDirectory(MainWindow.FileSystemNode node)
            {
                if(node is MainWindow.Archive a) {
                    foreach (var item in a.Children) {
                        if (item is MainWindow.FileItem f)
                            this.items.Add(f);
                        else if (item is MainWindow.FolderItem fd)
                            visitDirectory(fd);
                    }
                } else if(node is MainWindow.FolderItem fd2) {
                    foreach (var item in fd2.Children) {
                        if (item is MainWindow.FileItem f)
                            this.items.Add(f);
                        else if (item is MainWindow.FolderItem fd)
                            visitDirectory(fd);
                    }
                }
            }

            visitDirectory(archive);
            foreach (var item in this.items) {
                this.list.Items.Add(item);
            }

            this.txtFind.TextChanged += (s, e) => {
                this.list.Items.Clear();

                if (string.IsNullOrEmpty(this.txtFind.Text)) {
                    foreach (var item in this.items) {
                            this.list.Items.Add(item);
                    }
                } else foreach (var item in this.items) {
                    if (item.FullName.Contains(this.txtFind.Text))
                        this.list.Items.Add(item);
                }
            };

            RoutedEventHandler delDeleteSelection = (s, e) => {
                if (archive == null) return;
                string destination = new FileInfo(archive.FullName).Directory.FullName;

                // exclude working directory
                Guid taskGUID = Guid.NewGuid();
                string arguments = $"d \"{archive.FullName}\"";
                string startup = System.Windows.Forms.Application.StartupPath + @"\";
                var workingDir = Directory.CreateDirectory(startup + @"working\" + taskGUID);

                using (FileStream listfile = new FileStream(workingDir.FullName + @"\selected.txt",
                       FileMode.OpenOrCreate)) {
                    using (StreamWriter writer = new StreamWriter(listfile, Encoding.UTF8)) {
                        foreach (MainWindow.Item item in this.list.Items) {
                            writer.WriteLine(item.FullName);
                        }

                        writer.Flush();
                    }
                }

                arguments += $" @\"{workingDir.FullName + @"\selected.txt"}\"";

                ActionProcess proc = new ActionProcess(
                    arguments,
                    $"Deleting files from {archive.Name} ...",
                    $"Deleting archive file with command line options '{arguments}'"
                    );

                proc.Run();

                if (Directory.Exists(startup + @"working\" + taskGUID))
                    Directory.Delete(startup + @"working\" + taskGUID, true);
            };

            RoutedEventHandler delCopy = (s, e) => {
                if (archive == null) return;
                string destination = new FileInfo(archive.FullName).Directory.FullName;

                // exclude working directory
                Guid taskGUID = Guid.NewGuid();
                string arguments = $"x \"{archive.FullName}\"";
                string startup = System.Windows.Forms.Application.StartupPath + @"\";
                var workingDir = Directory.CreateDirectory(startup + @"working\" + taskGUID);

                // specify the working directory whether it is spf or not.
                arguments += $" -o\"{workingDir.FullName}\"";
                System.Collections.Specialized.StringCollection clipboardStrings = new System.Collections.Specialized.StringCollection();
                using (FileStream listfile = new FileStream(workingDir.FullName + @"\selected.txt",
                       FileMode.OpenOrCreate)) {
                    using (StreamWriter writer = new StreamWriter(listfile, Encoding.UTF8)) {

                        foreach (MainWindow.Item item in this.list.Items) {
                            string basepath = workingDir.FullName + "\\" + item.FullName.Replace("/", "\\");
                            if (isSPF)
                                basepath = workingDir.FullName + "\\" + item.FullName.Replace("/", "\\").Replace(":","_");

                            writer.WriteLine(item.FullName);
                            clipboardStrings.Add(basepath);
                        }

                        writer.Flush();
                    }
                }

                arguments += $" @\"{workingDir.FullName + @"\selected.txt"}\"";

                ActionProcess proc = new ActionProcess(
                    arguments,
                    $"Decompressing {archive.Name} ...",
                    $"Decompressing archive file with command line options '{arguments}'"
                    );

                proc.Run();

                // set to clipboard
                Clipboard.SetFileDropList(clipboardStrings);

                // delay deletion of the working directory
                MainWindow.delayedWorkingDir.Add(taskGUID);
            };

            RoutedEventHandler delCut = (s, e) => {
                delCopy(s, e);
                delDeleteSelection(s, e);
            };

            this.btnCopy.Click += (s, e) => { delCopy(s, e); this.Close(); };
            this.btnCut.Click += (s, e) => { delCut(s, e); this.Close(); };
            this.btnDelete.Click += (s, e) => { delDeleteSelection(s, e); this.Close(); };
            this.btnOK.Click += (s, e) => { this.Close(); };
        }

        List<MainWindow.Item> items = new List<MainWindow.Item>();

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
