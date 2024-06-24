using Aga.Controls.Tree;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using static Vanara.PInvoke.User32;
using System.Windows.Media;
using Microsoft.Win32;
using System.Collections;
using System.Drawing;

namespace Archiver
{
    public class FileEntry
    {
        public FileEntry(string fileSystemPath, string archivePath)
        {
            FileSystemPath = fileSystemPath;
            ArchivePath = archivePath;
        }

        public string FileSystemPath { get; set; }
        public string ArchivePath { get; set; }

        public string DisplayName
        {
            get
            {
                return System.IO.Path.GetFileName(FileSystemPath);
            }
        }

        public string Extension
        {
            get
            {
                return System.IO.Path.GetExtension(FileSystemPath);
            }
        }
    }

    public class FileEntryCollection : List<FileEntry>
    {
        public FileEntryCollection() { }

        public FileEntryCollection(List<(string directory, string stem)> files)
        {
            foreach (var item in files)
            {
                this.Add(new FileEntry(item.directory, item.stem));
            }
        }

        public FileEntryCollection(List<FileInfo> files, List<DirectoryInfo> dirs)
        {
            foreach (var item in files)
            {
                this.Add(new FileEntry(item.FullName, item.Name));
            }

            void walkDirectory(string relativeRoot, DirectoryInfo dir)
            {
                foreach (var item in dir.GetDirectories())
                {
                    try
                    {
                        if (!item.Attributes.HasFlag(FileAttributes.System))
                        {
                            // windows 11 regards .zip, .7z etc as directories, this will
                            // trigger unexpected behavior

                            if (item.Attributes.HasFlag(FileAttributes.Archive))
                            {
                                if (item.FullName.StartsWith(relativeRoot))
                                    this.Add(new FileEntry(item.FullName,
                                        item.FullName.Substring(relativeRoot.Length).Replace('\\', '/')));
                                else throw new Exception("Assertion Failed: the child directory doesn't contain the parent root.");
                            } else
                            {
                                walkDirectory(relativeRoot, item);
                            }
                        }

                    } catch { }
                }

                foreach (var item in dir.GetFiles())
                {
                    try
                    {
                        if (!item.Attributes.HasFlag(FileAttributes.System))
                        {
                            if (item.FullName.StartsWith(relativeRoot))
                                this.Add(new FileEntry(item.FullName,
                                    item.FullName.Substring(relativeRoot.Length).Replace('\\', '/')));
                            else throw new Exception("Assertion Failed: the child directory doesn't contain the parent root.");
                        }
                    } catch { }
                }
            }

            foreach (var directory in dirs)
            {
                if (directory.Attributes.HasFlag(FileAttributes.Archive))
                {
                    this.Add(new FileEntry(directory.FullName, directory.Name));
                } 
                else
                {
                    string parentalDirectoryPath = directory.Parent.FullName;
                    if (!parentalDirectoryPath.EndsWith("\\")) parentalDirectoryPath += "\\";
                    walkDirectory(parentalDirectoryPath, directory);
                }
            }
        }

        public int FilesCount
        {
            get
            {
                int num = 0;
                foreach (var item in this)
                {
                    if (!item.ArchivePath.Contains("/"))
                        num++;
                }
                return num;
            }
        }

        public int FoldersCount
        {
            get
            {
                List<string> folders = new List<string>();
                foreach (var item in this)
                {
                    if (item.ArchivePath.Contains("/"))
                    {
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

        public string Converter { get { return this.ToString(); } }
    }

    [ValueConversion(typeof(FileEntryCollection), typeof(string))]
    public class FileEntryCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FileEntryCollection items = (FileEntryCollection)value;
            return $"{items.FilesCount} files" +
                   (items.FoldersCount > 0 ? $" and {items.FoldersCount} folders, {items.Count} files in total" : "");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileTreeModel : ITreeModel
    {
        private FileTreeFolder root = new FileTreeFolder("/");
        public FileTreeModel(FileEntryCollection collection)
        {
            foreach(var entry in collection)
            {
                FileTree item = new FileTreeNode(entry);

                string[] fullpath = item.ArchiveName.Split('/');
                item.Name = fullpath.Last();
                FileTreeFolder? parent = null;
                string fullCascadeName = "";

                if (fullpath.Count() > 1)
                {
                    string cascadeName = fullpath[0];
                    fullCascadeName = fullCascadeName + cascadeName;
                    parent = (FileTreeFolder?) root.Children.Find((x) => {
                        return (x.Name == cascadeName && x.IsFolder);
                    });

                    if (parent == null)
                    {
                        parent = new FileTreeFolder(cascadeName);
                        root.Children.Add(parent);
                    }

                    for (int cascade = 1; cascade < fullpath.Length - 1; cascade++)
                    {
                        cascadeName = fullpath[cascade];
                        fullCascadeName = fullCascadeName + "/" + cascadeName;
                        FileTreeFolder? fi = (FileTreeFolder?) parent.Children.Find((x) => {
                            return x.Name == cascadeName && x.IsFolder;
                        });

                        if (fi == null)
                        {
                            fi = new FileTreeFolder(cascadeName);
                            parent.Children.Add(fi);
                        }

                        parent = fi;
                    }

                    if (item.IsFolder)
                    {
                        FileTreeFolder? found = (FileTreeFolder?) parent.Children.Find((x) => {
                            return (x.Name == item.Name && x.IsFolder);
                        });

                        if (found != null)
                        {
                            var folder = item as FileTreeFolder;
                            if (folder != null) folder.Children = found.Children;
                            parent.Children.Remove(found);
                        }
                    }

                    parent.Children.Add(item);
                    continue;
                }

                if (item.IsFolder)
                {
                    FileTreeFolder? found = (FileTreeFolder?) root.Children.Find((x) => {
                        return (x.Name == item.Name && x.IsFolder);
                    });

                    if (found != null)
                    {
                        var folder = item as FileTreeFolder;
                        if (folder != null) folder.Children = found.Children;
                        root.Children.Remove(found);
                    }
                }
                else
                {
                    FileTree? found = (FileTree?)root.Children.Find((x) => {
                        return (x.Name == item.Name && !x.IsFolder);
                    });

                    if (found == null)
                        root.Children.Add(item);
                }
            }
        }

        public bool HasChildren(object parent)
        {
            return parent is FileTreeFolder;
        }

        IEnumerable ITreeModel.GetChildren(object parent)
        {
            var parentFolder = parent as FileTreeFolder;
            if (parent == null)
                foreach (var item in this.root.Children)
                    yield return item;
            else if (parentFolder != null)
                foreach (var item in parentFolder.Children)
                    yield return item;
        }
    }

    public class FileTree
    {
        public bool IsFolder { get; set; }

        public string Name { get; set; } = "";
        public string LocalName { get; set; } = "";
        public string ArchiveName { get; set; } = "";

        public virtual ulong Size { get; set; }
        public virtual DateTime LastModified { get; set; }
        public virtual ImageSource? Icon { get; set; }

        public string SizeHumanFriendly { get { return Item.expressSize(this.Size); } }
    }

    public class FileTreeFolder : FileTree
    {
        ImageSourceConverter isc = new ImageSourceConverter();
        public FileTreeFolder(string folderName)
        {
            this.Name = folderName;
            this.IsFolder = true;
        }

        public List<FileTree> Children { get; set; } = new List<FileTree>();

        public override DateTime LastModified
        {
            get
            {
                return this.Children.Max(r => r.LastModified);
            }
        }

        public override ulong Size
        {
            get
            {
                ulong sum = 0;
                foreach (var item in this.Children)
                {
                    sum += item.Size;
                }
                return sum;
            }
        }

        public override ImageSource? Icon
        {
            get
            {
                return isc.ConvertFrom("pack://siteoforigin:,,,/resources/folder.ico") as ImageSource;
            }
        }
    }

    public class FileTreeNode : FileTree
    {
        public FileTreeNode(FileEntry entry)
        {
            this.LocalName = entry.FileSystemPath;
            this.ArchiveName = entry.ArchivePath;
            this.Name = entry.ArchivePath.Split('/').LastOrDefault("");

            FileInfo info = new FileInfo(entry.FileSystemPath);
            this.LastModified = info.LastWriteTime;
            this.Size = Convert.ToUInt64(info.Length);
            this.IsFolder = false;
        }

        public override ImageSource? Icon
        {
            get
            {
                return IconExtension.GetIconFromExtension(this.Name);
            }
        }
    }
}
