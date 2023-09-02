using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.IO;
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
using static Vanara.PInvoke.Shell32;

namespace Archiver
{
    /// <summary>
    /// Interaction logic for FileSelector.xaml
    /// </summary>
    public partial class FileSelector : Window
    {
        public FileSelector()
        {
            InitializeComponent();
            this.explorer.NavigationTarget = ShellObject.FromParsingName("C:");

            this.btnCancel.Click += (s, e) => {
                this.DialogResult = false;
                this.Close();
            };

            this.btnOK.Click += (s, e) => {

                this.SelectedDirectories.Clear();
                this.SelectedFiles.Clear();

                foreach (var item in this.explorer.SelectedItems) {
                    if (item.IsFileSystemObject) {
                        if (item is ShellFile file) {
                            this.SelectedFiles.Add(new FileInfo(file.ParsingName));
                        } else if (item is ShellFolder folder) {
                            // zip files are considered shell folders!
                            if (File.Exists(folder.ParsingName))
                                this.SelectedFiles.Add(new FileInfo(folder.ParsingName));
                            else
                                this.SelectedDirectories.Add(new DirectoryInfo(folder.ParsingName));
                        }
                    }
                }

                this.ParentalDirectory = new DirectoryInfo((explorer.NavigationLog.Last() as ShellFolder).ParsingName);
                this.SelectedEntries = new FileEntryCollection(SelectedFiles, SelectedDirectories);

                if (this.SelectedDirectories.Count > 0 || this.SelectedFiles.Count > 0) {
                    this.DialogResult = true;
                    this.Close();
                } else {
                    MessageBox.Show("You have not selected anything.");
                }
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
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
                return;
            }
            this.WindowState = WindowState.Minimized;
        }

        public DirectoryInfo ParentalDirectory { get; private set; } = null;
        public List<FileInfo> SelectedFiles { get; private set; } = new List<FileInfo>();
        public List<DirectoryInfo> SelectedDirectories { get; private set; } = new List<DirectoryInfo>();
        public FileEntryCollection SelectedEntries { get; private set; } = new FileEntryCollection();
    }

    public class FileEntry
    {
        public FileEntry(string fileSystemPath, string archivePath)
        {
            FileSystemPath = fileSystemPath;
            ArchivePath = archivePath;
        }

        public string FileSystemPath { get; set; }
        public string ArchivePath { get; set; }

        public string DisplayName { 
            get {
                return System.IO.Path.GetFileName(FileSystemPath);
            }
        }

        public string Extension {
            get {
                return System.IO.Path.GetExtension(FileSystemPath);
            }
        }
    }

    public class FileEntryCollection : List<FileEntry>
    {
        public FileEntryCollection() { }

        public FileEntryCollection(List<(string directory, string stem)> files)
        {
            foreach (var item in files) {
                this.Add(new FileEntry(item.directory, item.stem));
            }
        }

        public FileEntryCollection(List<FileInfo> files, List<DirectoryInfo> dirs)
        {
            foreach (var item in files) {
                this.Add(new FileEntry(item.FullName, item.Name));
            }

            void walkDirectory(string relativeRoot, DirectoryInfo dir)
            {
                foreach (var item in dir.GetDirectories()) {
                    try {
                        if (!item.Attributes.HasFlag(FileAttributes.System)) {
                            // windows 11 regards .zip, .7z etc as directories, this will
                            // trigger unexpected behavior
                            if (item.Attributes.HasFlag(FileAttributes.Archive)) {
                                if (item.FullName.StartsWith(relativeRoot))
                                    this.Add(new FileEntry(item.FullName,
                                        item.FullName.Substring(relativeRoot.Length).Replace('\\', '/')));
                                else throw new Exception("Assertion Failed: the child directory doesn't contain the parent root.");
                            } 
                            else {
                                walkDirectory(relativeRoot, item); 
                            }
                        }

                    } catch { }
                }

                foreach (var item in dir.GetFiles()) {
                    try {
                        if (!item.Attributes.HasFlag(FileAttributes.System)) {
                            if (item.FullName.StartsWith(relativeRoot))
                                this.Add(new FileEntry(item.FullName,
                                    item.FullName.Substring(relativeRoot.Length).Replace('\\', '/')));
                            else throw new Exception("Assertion Failed: the child directory doesn't contain the parent root.");
                        }
                    } catch { }
                }
            }

            foreach (var directory in dirs) {
                if (directory.Attributes.HasFlag(FileAttributes.Archive)) {
                    this.Add(new FileEntry(directory.FullName, directory.Name));
                } else {
                    string parentalDirectoryPath = directory.Parent.FullName;
                    if (!parentalDirectoryPath.EndsWith("\\")) parentalDirectoryPath += "\\";
                    walkDirectory(parentalDirectoryPath, directory);
                }
            }
        }

        public int FilesCount
        {
            get {
                int num = 0;
                foreach(var item in this) {
                    if (!item.ArchivePath.Contains("/"))
                        num++;
                }
                return num;
            }
        }

        public int FoldersCount
        {
            get {
                List<string> folders = new List<string>();
                foreach (var item in this) {
                    if (item.ArchivePath.Contains("/")) {
                        var folder = item.ArchivePath.Split('/')[0];
                        if (!folders.Contains(folder))
                            folders.Add(folder);
                    }
                }

                return folders.Count;
            }
        }

        public override string ToString()
        {
            return $"{FilesCount} files" +
                   (FoldersCount > 0 ? $" and {FoldersCount} folders, {this.Count} files in total" : "");
        }
    }
}
