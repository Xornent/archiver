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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Interop;
using System.ComponentModel;
using System.Security.Cryptography;

namespace Archiver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<ComboBox> comboDirs = new List<ComboBox>();
        public MainWindow()
        {
            InitializeComponent();
            this.StateChanged += (s, e) => {
                if (this.WindowState == WindowState.Maximized)
                    windowGrid.Margin = new Thickness(8);
                else
                    windowGrid.Margin = new Thickness(0);
            };

            this.list.MouseDoubleClick += (s, e) => {
                if (this.list.SelectedItems.Count == 1) {
                    object item = this.list.SelectedItem;
                    if (item is FolderItem folder) {
                        Navigate(folder);
                    }
                }
            };

            this.dirCombo.Click += (s, e) => {
                if (this.currentArchive == null) return;
                if (comboSuppress) return;
                Navigate(this.currentArchive);
            };

            this.menuSortDescending.Click += (s, e) => {
                resetSortDirectionUI();
                this.menuSortDescending.IsChecked = true;
                this.direction = ListSortDirection.Descending;
                this.sort();
            };

            this.menuSortAscending.Click += (s, e) => {
                resetSortDirectionUI();
                this.menuSortAscending.IsChecked = true;
                this.direction = ListSortDirection.Ascending;
                this.sort();
            };

            this.menuSortbyName.Click += (s, e) => {
                resetSortbyUI();
                this.menuSortbyName.IsChecked = true;
                this.sortProperty = "Name";
                this.sort();
            };

            this.menuSortbyCreation.Click += (s, e) => {
                resetSortbyUI();
                this.menuSortbyCreation.IsChecked = true;
                this.sortProperty = "DateCreation";
                this.sort();
            };

            this.menuSortbyModified.Click += (s, e) => {
                resetSortbyUI();
                this.menuSortbyModified.IsChecked = true;
                this.sortProperty = "DateModified";
                this.sort();
            };

            this.menuPrev.Click += (s, e) => {
                var last = this.previous.Last();
                this.next.Insert(0, last);
                this.previous.RemoveAt(this.previous.Count - 1);
                this.menuPrev.IsEnabled = this.previous.Count > 1;
                this.menuNext.IsEnabled = this.next.Count > 0;

                this.Navigate(this.previous.Last(), true);
            };

            this.menuNext.Click += (s, e) => {
                var first = this.next[0];
                this.previous.Add(first);
                this.next.RemoveAt(0);
                this.menuPrev.IsEnabled = this.previous.Count > 1;
                this.menuNext.IsEnabled = this.next.Count > 0;

                this.Navigate(first, true);
            };

            this.menuUplevel.Click += (s, e) => {
                if (currentArchive == null) return;
                if (comboDirs.Count > 1) {
                    this.Navigate((FileSystemNode)comboDirs[comboDirs.Count - 2].SelectedItem, true);
                } else this.Navigate(currentArchive, true);
            };

            this.menuRootdir.Click += (s, e) => {
                if (currentArchive == null) return;
                this.Navigate(currentArchive, true);
            };

            this.list.SelectionChanged += (s, e) => {
                if (currentArchive == null) return;
                if (this.list.SelectedItems.Count == 0) return;
                if (this.list.SelectedItems.Count == 1) {
                    FileSystemNode node = this.list.SelectedItems[0] as FileSystemNode ?? currentArchive;

                    this.imgIcon.Source = node.Icon;
                    if (node is Archive arch) {
                        this.lblDesc.Text = arch.Name + " (" + arch.Children.Count + " items)";
                        this.tArchiveType.Text = arch.Type;
                        this.sArchiveType.Visibility = Visibility.Visible;
                        this.tPhysicalSize.Text = exprSize(arch.PhysicalSize);
                        this.sPhysicalSize.Visibility = Visibility.Visible;
                        this.sActualSize.Visibility = Visibility.Collapsed;
                        this.sAttributes.Visibility = Visibility.Collapsed;
                        this.sChar.Visibility = Visibility.Collapsed;
                        this.sCompressedSize.Visibility = Visibility.Collapsed;
                        this.sCompressionMethod.Visibility = Visibility.Collapsed;
                        this.sCRC.Visibility = Visibility.Collapsed;
                        this.sDateAccessed.Visibility = Visibility.Collapsed;
                        this.sDateCreation.Visibility = Visibility.Collapsed;
                        this.sDateModified.Visibility = Visibility.Collapsed;
                        this.sEncrypted.Visibility = Visibility.Collapsed;
                        this.sHostOS.Visibility = Visibility.Collapsed;
                        this.sVer.Visibility = Visibility.Collapsed;
                        this.sVol.Visibility = Visibility.Collapsed;

                    } else if (node is Item it) {

                        this.sActualSize.Visibility = Visibility.Collapsed;
                        this.sAttributes.Visibility = Visibility.Collapsed;
                        this.sChar.Visibility = Visibility.Collapsed;
                        this.sCompressedSize.Visibility = Visibility.Collapsed;
                        this.sCompressionMethod.Visibility = Visibility.Collapsed;
                        this.sCRC.Visibility = Visibility.Collapsed;
                        this.sDateAccessed.Visibility = Visibility.Collapsed;
                        this.sDateCreation.Visibility = Visibility.Collapsed;
                        this.sDateModified.Visibility = Visibility.Collapsed;
                        this.sEncrypted.Visibility = Visibility.Collapsed;
                        this.sHostOS.Visibility = Visibility.Collapsed;
                        this.sVer.Visibility = Visibility.Collapsed;
                        this.sVol.Visibility = Visibility.Collapsed;

                        if ((it.Name ?? "").Contains(".")) {
                            string ext = (it.Name ?? "").Split('.').Last();
                            if (!File.Exists(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext))
                                File.Create(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext);

                            var icon = GetIconFromFile(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext, Vanara.PInvoke.Shell32.SHIL.SHIL_JUMBO);
                            if (icon?.Width > 0) {
                                this.imgIcon.Source = icon.ToImageSource();
                            }
                        }

                        if (it.IsFolder)
                            this.lblDesc.Text = it.Name + " (" + (it as FolderItem)?.Children.Count + " items)";
                        else
                            this.lblDesc.Text = it.Name;

                        this.sArchiveType.Visibility = Visibility.Collapsed;
                        this.sPhysicalSize.Visibility = Visibility.Collapsed;

                        if (it.Size != default(long)) {
                            this.sActualSize.Visibility = Visibility.Visible;
                            this.tActualSize.Text = exprSize(it.Size);
                        }

                        if (it.Attributes != default(string)) {
                            this.sAttributes.Visibility = Visibility.Visible;
                            this.tAttributes.Text = it.Attributes;
                        }

                        if (it.Characteristics != default(string)) {
                            this.sChar.Visibility = Visibility.Visible;
                            this.tChar.Text = it.Characteristics;
                        }

                        if (it.CompressedSize != default(long)) {
                            this.sCompressedSize.Visibility = Visibility.Visible;
                            this.tCompressedSize.Text = exprSize(it.CompressedSize);
                        }

                        if (it.Method != default(string)) {
                            this.sCompressionMethod.Visibility = Visibility.Visible;
                            this.tCompressionMethod.Text = it.Method;
                        }

                        if (it.CRC != default(string)) {
                            this.sCRC.Visibility = Visibility.Visible;
                            this.tCRC.Text = it.CRC;
                        }

                        if (it.DateAccessed != default(DateTime?)) {
                            this.sDateAccessed.Visibility = Visibility.Visible;
                            this.tDateAccessed.Text = it.DateAccessed.ToString();
                        }
                        if (it.DateCreation != default(DateTime?)) {
                            this.sDateCreation.Visibility = Visibility.Visible;
                            this.tDateCreation.Text = it.DateCreation.ToString();
                        }
                        if (it.DateModified != default(DateTime?)) {
                            this.sDateModified.Visibility = Visibility.Visible;
                            this.tDateModified.Text = it.DateModified.ToString();
                        }

                        if (it.IsEncrypted != null) {
                            this.sEncrypted.Visibility = Visibility.Collapsed;
                            this.tEncrypted.Text = it.IsEncrypted.ToString();
                        }

                        if (it.HostOS != default(string)) {
                            this.sHostOS.Visibility = Visibility.Visible;
                            this.tHostOS.Text = it.HostOS;
                        }

                        if (it.Version != default(string)) {
                            this.sVer.Visibility = Visibility.Visible;
                            this.tVer.Text = it.Version;
                        }

                        if (it.VolumeId != default(int?)) {
                            this.sVol.Visibility = Visibility.Visible;
                            this.tVol.Text = it.VolumeId.ToString();
                        }

                        this.tComment.Text = it.Comment ?? "";
                    }
                }
            };

            this.mnuAbout.Click += (s, e) => {
                (new About()).ShowDialog();
            };

            this.mnuDecompress.Click += (s, e) => {
                if (currentArchive != null)
                    (new Extract(this.currentArchive)).ShowDialog();
            };
        }

        public static Vanara.PInvoke.HICON GetIconDefault(string fileName)
        {
            Vanara.PInvoke.Shell32.SHFILEINFO info = new Vanara.PInvoke.Shell32.SHFILEINFO();
            IntPtr iconIntPtr = Vanara.PInvoke.Shell32.SHGetFileInfo(
                fileName, 0, ref info, (int)Marshal.SizeOf(info),
                Vanara.PInvoke.Shell32.SHGFI.SHGFI_ICON | Vanara.PInvoke.Shell32.SHGFI.SHGFI_OPENICON);
            if (iconIntPtr == IntPtr.Zero)
                return IntPtr.Zero;
            return info.hIcon;
        }

        public static int GetIconIndex(string fileName)
        {
            Vanara.PInvoke.Shell32.SHFILEINFO info = new Vanara.PInvoke.Shell32.SHFILEINFO();
            IntPtr iconIntPtr = Vanara.PInvoke.Shell32.SHGetFileInfo(
                fileName, 0, ref info, (int)Marshal.SizeOf(info),
                Vanara.PInvoke.Shell32.SHGFI.SHGFI_SYSICONINDEX | Vanara.PInvoke.Shell32.SHGFI.SHGFI_OPENICON);
            if (iconIntPtr == IntPtr.Zero)
                return -1;
            return info.iIcon;
        }

        public static System.Drawing.Icon GetIcon(int iIcon, Vanara.PInvoke.Shell32.SHIL flag)
        {
            object list = null;
            Guid theGuid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950"); // IID_IImageList
            // Acquire the system image list
            Vanara.PInvoke.Shell32.SHGetImageList(flag, in theGuid, out list);

            Vanara.PInvoke.ComCtl32.IImageList imglist = (Vanara.PInvoke.ComCtl32.IImageList)list;
            var hIcon = imglist.GetIcon(iIcon,
                Vanara.PInvoke.ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT |
                Vanara.PInvoke.ComCtl32.IMAGELISTDRAWFLAGS.ILD_IMAGE);

            return System.Drawing.Icon.FromHandle(hIcon.DangerousGetHandle());
        }

        public static System.Drawing.Icon GetIconFromFile(string fileName, Vanara.PInvoke.Shell32.SHIL flag)
        {
            // return System.Drawing.Icon.FromHandle(GetIconIndex(fileName).DangerousGetHandle());
            return GetIcon(GetIconIndex(fileName), flag);
        }

        private string exprSize(long sizeB)
        {
            string expr = sizeB + " Bytes";

            float kib = sizeB / 1024f;
            if (kib > 1) expr = kib.ToString("F2") + " kiB";
            else return expr;

            float mib = kib / 1024f;
            if (mib > 1) expr = mib.ToString("F2") + " miB";
            else return expr;

            float gib = mib / 1024f;
            if (gib > 1) expr = gib.ToString("F2") + " giB";
            return expr;
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void bdClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void bdMinimize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
                return;
            }
            this.WindowState = WindowState.Minimized;
        }

        private void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "All|*.*";
            dialog.Title = "Open archive file";

            if (dialog.ShowDialog() == true) {
                openFile(dialog.FileName);
            }
        }

        private void openFile(string path)
        {
            string startup = System.Windows.Forms.Application.StartupPath;
            string zpath = startup + @"\7z\x64\7z-unicode.exe";
            string param = "l \"" + path + "\" -slt -y";

            Process proc = new Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.FileName = zpath;
            proc.StartInfo.Arguments = param;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardError = true;

            bool suc = proc.Start();

            if (proc != null) {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                output = output.Parse7zUnicode();

                System.IO.FileInfo info = new FileInfo(path);

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) => {
                    ImageSourceConverter isc = new ImageSourceConverter();
                    var tempArchive = new Archive();

                    tempArchive.Name = info.Name;
                    tempArchive.Icon = (ImageSource)isc.ConvertFrom("pack://siteoforigin:,,,/resources/brief.ico");

                    bool beginArchiveHeader = false;
                    bool beginContentHeader = false;
                    Item item = null;

                    foreach (var line in output.Replace("\r\n", "\n").Split('\n')) {
                        if (line == "--") {
                            beginArchiveHeader = true;
                            continue;
                        } else if (line == "----------") {
                            beginContentHeader = true;
                            beginArchiveHeader = false;
                            item = new FileItem();
                            continue;
                        }

                        if (beginArchiveHeader) {
                            if (string.IsNullOrEmpty(line)) continue;

                            string[] pair = line.Split('=');
                            string content = pair[1].Trim();
                            bool emptyContent = string.IsNullOrEmpty(content);
                            switch (pair[0].Trim().ToLower()) {
                                case "path": tempArchive.FullName = content; break;
                                case "type": tempArchive.Type = content; break;
                                case "physical size": tempArchive.PhysicalSize = long.Parse(content); break;
                                default:
                                    break;
                            }
                            continue;
                        }

                        if (beginContentHeader) {
                            if (string.IsNullOrWhiteSpace(line)) {
                                if (item != null && !string.IsNullOrEmpty(item.FullName)) {
                                    if (item.IsFolder) {
                                        FolderItem temp = new FolderItem() {
                                            Attributes = item.Attributes,
                                            Characteristics = item.Characteristics,
                                            Comment = item.Comment,
                                            CompressedSize = item.CompressedSize,
                                            CRC = item.CRC,
                                            DateAccessed = item.DateAccessed,
                                            DateCreation = item.DateCreation,
                                            DateModified = item.DateModified,
                                            FullName = item.FullName,
                                            IsFolder = item.IsFolder,
                                            HostOS = item.HostOS,
                                            IsEncrypted = item.IsEncrypted,
                                            Method = item.Method,
                                            VolumeId = item.VolumeId,
                                            Name = item.Name,
                                            Offset = item.Offset,
                                            Size = item.Size,
                                            Version = item.Version
                                        };

                                        item = temp;
                                    }

                                    string[] fullpath = item.FullName.Replace("\\", "/").Split('/');
                                    item.Name = fullpath.Last();
                                    FolderItem parent = null;
                                    string fullCascadeName = "";

                                    // extract icons

                                    if (item.IsFolder) {
                                        item.Icon = (ImageSource)isc.ConvertFrom("pack://siteoforigin:,,,/resources/folder.ico");
                                    } else {
                                        if (item.Name.Contains(".")) {
                                            this.Dispatcher.Invoke(new Action(() => {
                                                string ext = item.Name.Split('.').Last();
                                                if (!File.Exists(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext))
                                                    File.Create(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext);
                                                var icon = GetIconFromFile(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext, Vanara.PInvoke.Shell32.SHIL.SHIL_SMALL);
                                                //var icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext);
                                                if (icon?.Width > 0)
                                                    item.Icon = icon.ToImageSource();
                                            }));
                                        }
                                    }

                                    if (fullpath.Count() > 1) {
                                        string cascadeName = fullpath[0];
                                        fullCascadeName = fullCascadeName + cascadeName;
                                        parent = (FolderItem)tempArchive.Children.Find((x) => { return (x.Name == cascadeName && x.IsFolder); });
                                        if (parent == null) {
                                            parent = new FolderItem() {
                                                Name = cascadeName,
                                                FullName = fullCascadeName,
                                                IsFolder = true,
                                                Icon = (ImageSource)isc.ConvertFrom("pack://siteoforigin:,,,/resources/folder.ico")
                                            };
                                            tempArchive.Children.Add(parent);
                                        }

                                        for (int cascade = 1; cascade < fullpath.Length - 1; cascade++) {
                                            cascadeName = fullpath[cascade];
                                            fullCascadeName = fullCascadeName + "/" + cascadeName;
                                            FolderItem fi = (FolderItem)parent.Children.Find((x) => { return x.Name == cascadeName && x.IsFolder; });
                                            if (fi == null) {
                                                fi = new FolderItem() {
                                                    Name = cascadeName,
                                                    FullName = fullCascadeName,
                                                    IsFolder = true,
                                                    Icon = (ImageSource)isc.ConvertFrom("pack://siteoforigin:,,,/resources/folder.ico")
                                                };
                                                parent.Children.Add(fi);
                                            }

                                            parent = fi;
                                        }

                                        if (item.IsFolder) {
                                            FolderItem found = (FolderItem)parent.Children.Find((x) => {
                                                return (x.Name == item.Name && x.IsFolder);
                                            });

                                            if (found != null) {
                                                var folder = item as FolderItem;
                                                if (folder != null) folder.Children = found.Children;
                                                parent.Children.Remove(found);
                                            }
                                        }

                                        parent.Children.Add(item);
                                        item = new FileItem();
                                        continue;
                                    }

                                    if (item.IsFolder) {
                                        FolderItem found = (FolderItem)tempArchive.Children.Find((x) => {
                                            return (x.Name == item.Name && x.IsFolder);
                                        });

                                        if (found != null) {
                                            var folder = item as FolderItem;
                                            if (folder != null) folder.Children = found.Children;
                                            tempArchive.Children.Remove(found);
                                        }
                                    }

                                    tempArchive.Children.Add(item);
                                    item = new FileItem();
                                }

                            } else {
                                string[] pair = line.Split('=');
                                string content = pair[1].Trim();
                                bool emptyContent = string.IsNullOrEmpty(content);

                                if (item == null) continue;
                                if (emptyContent) continue;
                                switch (pair[0].Trim().ToLower()) {
                                    case "path":
                                        item.FullName = content;
                                        var substring = content;
                                        if (substring.Length > 50)
                                            substring = "... " + content.Substring(content.Length - 50);
                                        worker.ReportProgress(0, (
                                            "Opening archive ...",
                                            "Reading archived content information from",
                                            substring
                                            ));
                                        break;
                                    case "folder": item.IsFolder = content == "+"; break;
                                    case "size": item.Size = long.Parse(content); break;
                                    case "packed size": item.CompressedSize = long.Parse(content); break;
                                    case "modified": if (!emptyContent) item.DateModified = DateTime.Parse(content); break;
                                    case "created": if (!emptyContent) item.DateCreation = DateTime.Parse(content); break;
                                    case "accessed": if (!emptyContent) item.DateAccessed = DateTime.Parse(content); break;
                                    case "attributes":
                                        item.Attributes = content;
                                        if (content.Contains("D"))
                                            item.IsFolder = true;
                                        break;
                                    case "encrypted": item.IsEncrypted = content == "+"; break;
                                    case "comment": item.Comment = content; break;
                                    case "crc": item.CRC = content; break;
                                    case "method": item.Method = content; break;
                                    case "characteristics": item.Characteristics = content; break;
                                    case "host os": item.HostOS = content; break;
                                    case "version": item.Version = content; break;
                                    case "volume index": item.VolumeId = int.Parse(content); break;
                                    case "offset": item.Offset = int.Parse(content); break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    this.Dispatcher.Invoke(new Action(() => {
                        this.currentArchive = tempArchive;
                        this.dirCombo.Content = (currentArchive);
                        this.lblTitle.Text = "Archiver · " + System.IO.Path.GetFileName(path);

                        // now fininshes current archive parsing.
                        Navigate(currentArchive);

                        {
                            this.imgIcon.Source = currentArchive.Icon;
                            this.lblDesc.Text = currentArchive.Name + " (" + currentArchive.Children.Count + " items)";
                            this.tArchiveType.Text = currentArchive.Type;
                            this.sArchiveType.Visibility = Visibility.Visible;
                            this.tPhysicalSize.Text = exprSize(currentArchive.PhysicalSize);
                            this.sPhysicalSize.Visibility = Visibility.Visible;
                            this.sActualSize.Visibility = Visibility.Collapsed;
                            this.sAttributes.Visibility = Visibility.Collapsed;
                            this.sChar.Visibility = Visibility.Collapsed;
                            this.sCompressedSize.Visibility = Visibility.Collapsed;
                            this.sCompressionMethod.Visibility = Visibility.Collapsed;
                            this.sCRC.Visibility = Visibility.Collapsed;
                            this.sDateAccessed.Visibility = Visibility.Collapsed;
                            this.sDateCreation.Visibility = Visibility.Collapsed;
                            this.sDateModified.Visibility = Visibility.Collapsed;
                            this.sEncrypted.Visibility = Visibility.Collapsed;
                            this.sHostOS.Visibility = Visibility.Collapsed;
                            this.sVer.Visibility = Visibility.Collapsed;
                            this.sVol.Visibility = Visibility.Collapsed;
                        }
                    }
                        ));

                    this.Dispatcher.Invoke(new Action(() => {
                        this.previous.Clear();
                        this.next.Clear();
                        this.menuRootdir.IsEnabled = true;
                        this.menuUplevel.IsEnabled = true;
                        this.splashScreen.Visibility = Visibility.Hidden;
                        this.gridList.Visibility = Visibility.Visible;
                        this.gridDetails.Visibility = Visibility.Visible;

                        this.mnuDecompress.IsEnabled = true;
                    }));
                };

                Loader loader = new Loader(worker);
                loader.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                loader.ResizeMode = ResizeMode.NoResize;
                loader.ShowDialog();
            }
        }

        bool comboSuppress = false;
        private void Navigate(FileSystemNode directory, bool suppress = false)
        {
            if (currentArchive == null) return;

            if (!suppress) {
                this.previous.Add(directory);
                this.menuPrev.IsEnabled = this.previous.Count > 1;
                this.menuNext.IsEnabled = this.next.Count > 0;
            }

            comboSuppress = true;
            if (directory is Archive archive) {
                this.list.Items.Clear();
                foreach (var item in archive.Children) {
                    if (item is FolderItem)
                        this.list.Items.Add(item);
                }

                foreach (var item in archive.Children) {
                    if (item is FileItem)
                        this.list.Items.Add(item);
                }

                this.comboDirs.Clear();
                this.stackDirs.Children.RemoveRange(1, this.stackDirs.Children.Count - 1);
                this.dirCombo.Content = archive;

            } else if (directory is FolderItem folder) {
                this.list.Items.Clear();
                foreach (var item in folder.Children) {
                    if (item is FolderItem)
                        this.list.Items.Add(item);
                }

                foreach (var item in folder.Children) {
                    if (item is FileItem)
                        this.list.Items.Add(item);
                }

                refreshDirectoryCombo(folder.FullName, currentArchive);

            } else if (directory is FileItem file) {
                Navigate(file.FullName, currentArchive);
                refreshDirectoryCombo(file.FullName, currentArchive);
            }

            comboSuppress = false;
        }

        private void Navigate(string relativePath, Archive archive)
        {
            string[] fullpath = relativePath.Replace("\\", "/").Split('/');
            FolderItem parent = null;
            string folderName = fullpath.Last();
            string fullCascadeName = "";

            if (fullpath.Count() > 1) {
                string cascadeName = fullpath[0];
                fullCascadeName = fullCascadeName + cascadeName;
                parent = (FolderItem)archive.Children.Find((x) => { return (x.Name == cascadeName && x.IsFolder); });
                if (parent == null) return;

                for (int cascade = 1; cascade < fullpath.Length - 1; cascade++) {
                    cascadeName = fullpath[cascade];
                    fullCascadeName = fullCascadeName + "/" + cascadeName;
                    FolderItem fi = (FolderItem)parent.Children.Find((x) => { return x.Name == cascadeName; });
                    if (fi == null) return;

                    parent = fi;
                }

                FolderItem found = (FolderItem)parent.Children.Find((x) => {
                    return (x.Name == folderName && x.IsFolder);
                });

                if (found != null) Navigate(found);
                else Navigate(parent);

            } else {

                FolderItem found = (FolderItem)archive.Children.Find((x) => {
                    return (x.Name == folderName && x.IsFolder);
                });

                if (found != null) Navigate(found);
                else Navigate(archive);
            }
        }

        private TextBlock createArrow()
        {
            TextBlock block = new TextBlock();
            block.FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons");
            block.Text = "\ue76C";
            block.FontSize = 18;
            block.HorizontalAlignment = HorizontalAlignment.Left;
            block.VerticalAlignment = VerticalAlignment.Center;
            block.Margin = new Thickness(5, 0, 5, 0);
            return block;
        }

        private void refreshDirectoryCombo(string relativePath, Archive archive)
        {
            comboSuppress = true;
            string[] fullpath = relativePath.Replace("\\", "/").Split('/');
            FolderItem parent = null;
            string folderName = fullpath.Last();
            string fullCascadeName = "";

            this.dirCombo.Content = archive;
            this.comboDirs.Clear();
            this.stackDirs.Children.RemoveRange(1, this.stackDirs.Children.Count - 1);

            if (fullpath.Count() > 1) {
                string cascadeName = fullpath[0];
                fullCascadeName = fullCascadeName + cascadeName;
                parent = (FolderItem)archive.Children.Find((x) => { return (x.Name == cascadeName && x.IsFolder); });
                int parentId = -1; int tempId = 0;
                if (parent == null) return;

                ComboBox combo = new ComboBox();
                combo.ItemTemplate = (DataTemplate)this.Resources["dirTemplate"];
                foreach (var item in archive.Children) {
                    if (item.IsFolder) {
                        combo.Items.Add(item);
                        if (item.Name == cascadeName) { parentId = tempId; }
                        tempId++;
                    }
                }
                combo.SelectedIndex = parentId;
                this.stackDirs.Children.Add(createArrow());
                this.stackDirs.Children.Add(combo);
                this.comboDirs.Add(combo);
                combo.SelectionChanged += this.comboSelectionChanged;

                for (int cascade = 1; cascade < fullpath.Length - 1; cascade++) {
                    cascadeName = fullpath[cascade];
                    fullCascadeName = fullCascadeName + "/" + cascadeName;
                    FolderItem fi = (FolderItem)parent.Children.Find((x) => { return x.Name == cascadeName && x.IsFolder; });
                    int fid = 0; int tid0 = 0;
                    if (fi == null) return;

                    ComboBox combo2 = new ComboBox();
                    combo2.ItemTemplate = (DataTemplate)this.Resources["dirTemplate"];

                    foreach (var item in parent.Children) {
                        if (item.IsFolder) {
                            combo2.Items.Add(item);
                            if (item.Name == cascadeName) { fid = tid0; }
                            tid0++;
                        }
                    }
                    combo2.SelectedIndex = fid;
                    this.stackDirs.Children.Add(createArrow());
                    this.stackDirs.Children.Add(combo2);
                    this.comboDirs.Add(combo2);
                    combo2.SelectionChanged += this.comboSelectionChanged;

                    parent = fi;
                }

                FolderItem found = (FolderItem)parent.Children.Find((x) => {
                    return (x.Name == folderName && x.IsFolder);
                });

                if (found != null) {
                    ComboBox combo2 = new ComboBox();
                    combo2.ItemTemplate = (DataTemplate)this.Resources["dirTemplate"];
                    int foundId = 0; int tid = 0;
                    foreach (var item in parent.Children) {
                        if (item.IsFolder) {
                            combo2.Items.Add(item);
                            if (item.Name == folderName) { foundId = tid; }
                            tid++;
                        }
                    }
                    combo2.SelectedIndex = foundId;
                    this.stackDirs.Children.Add(createArrow());
                    this.stackDirs.Children.Add(combo2);
                    this.comboDirs.Add(combo2);
                    combo2.SelectionChanged += this.comboSelectionChanged;
                }

            } else {

                FolderItem found = (FolderItem)archive.Children.Find((x) => {
                    return (x.Name == folderName && x.IsFolder);
                });

                if (found != null) {
                    ComboBox combo2 = new ComboBox();
                    combo2.ItemTemplate = (DataTemplate)this.Resources["dirTemplate"];
                    int foundId = 0; int tid = 0;
                    foreach (var item in archive.Children) {
                        if (item.IsFolder) {
                            combo2.Items.Add(item);
                            if (item.Name == folderName) { foundId = tid; }
                            tid++;
                        }
                    }
                    combo2.SelectedIndex = foundId;
                    this.stackDirs.Children.Add(createArrow());
                    this.stackDirs.Children.Add(combo2);
                    this.comboDirs.Add(combo2);
                    combo2.SelectionChanged += this.comboSelectionChanged;
                }
            }

            comboSuppress = false;
        }

        private void comboSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.currentArchive == null) return;
            if (comboSuppress) return;
            if ((e.Source as ComboBox)?.SelectedItem is FileSystemNode node)
                Navigate(node);
        }

        ListSortDirection direction = ListSortDirection.Ascending;
        string sortProperty = "Name";

        private void sort()
        {
            if (this.currentArchive == null) return;
            SortDescription desc = new SortDescription(sortProperty, direction);
            this.list.Items.SortDescriptions.Clear();
            this.list.Items.SortDescriptions.Add(desc);
        }

        private void resetSortbyUI()
        {
            this.menuSortbyCreation.IsChecked = false;
            this.menuSortbyModified.IsChecked = false;
            this.menuSortbyName.IsChecked = false;
        }

        private void resetSortDirectionUI()
        {
            this.menuSortAscending.IsChecked = false;
            this.menuSortDescending.IsChecked = false;
        }

        Archive currentArchive = null;

        List<FileSystemNode> previous = new List<FileSystemNode>();
        List<FileSystemNode> next = new List<FileSystemNode>();

        public class Item : FileSystemNode
        {
            public string FullName { get; set; } = "";
            public bool IsFolder { get; set; }
            public long Size { get; set; }
            public long CompressedSize { get; set; }
            public DateTime? DateModified { get; set; }
            public DateTime? DateCreation { get; set; }
            public DateTime? DateAccessed { get; set; }
            public string Attributes { get; set; }
            public bool? IsEncrypted { get; set; }
            public string CRC { get; set; }
            public string Method { get; set; }
            public string Characteristics { get; set; }
            public string HostOS { get; set; }
            public string Version { get; set; }
            public int? VolumeId { get; set; }
            public long? Offset { get; set; }
            public string Comment { get; set; }
        }

        public class FolderItem : Item
        {
            public List<Item> Children { get; set; } = new List<Item>();
        }

        public class FileItem : Item
        {

        }

        public class Archive : FileSystemNode
        {
            public List<Item> Children { get; set; } = new List<Item>();
            public string FullName { get; set; } = "";
            public string Type { get; set; } = "";
            public long PhysicalSize { get; set; }
        }

        public class FileSystemNode
        {
            public string Name { get; set; }
            public ImageSource Icon { get; set; }
        }

        private void menuCreate_Click(object sender, RoutedEventArgs e)
        {
            (new Compress()).Show();
        }
    }

    internal static class IconUtilities
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static ImageSource ToImageSource(this Icon icon)
        {
            Bitmap bitmap = icon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap)) {
                throw new Win32Exception();
            }

            return wpfBitmap;
        }

        public static string Parse7zUnicode(this string output)
        {
            // parse unicode out
            List<char> unicode = new List<char>();
            char lastChar = (char)0;
            bool isNumericMode = false;
            for (int i = 0; i < output.Length; i++) {
                char c = output[i];
                if (c == '?') {
                    if (i + 1 < output.Length)
                        if (output[i + 1] == '#') {
                            isNumericMode = true;
                            i++;
                            continue;
                        }
                }

                if (c == '#') {
                    if (i + 1 < output.Length)
                        if (output[i + 1] == '?') {
                            isNumericMode = false;
                            i++;
                            continue;
                        }
                }

                if (isNumericMode) {
                    if (c >= '0' && c <= '9') {
                        lastChar = Convert.ToChar(lastChar * 10 + (c - '0'));
                    } else if (c == ' ') {
                        if (lastChar != 0)
                            unicode.Add(lastChar);
                        lastChar = (char)0;
                    }
                } else {
                    unicode.Add(c);
                }
            }
            return new string(unicode.ToArray());
        }
    }
}
