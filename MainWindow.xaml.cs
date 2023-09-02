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
using System.Reflection;
using System.Runtime.CompilerServices;
using Archiver.Properties;
using static Archiver.MainWindow;

namespace Archiver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<ComboBox> comboDirs = new List<ComboBox>();
        public ContextMenu ctxMenu;
        public List<Guid> delayedWorkingDir = new List<Guid>();
        public MainWindow()
        {
            InitializeComponent();

            ctxMenu = this.Resources["ctxMenu"] as ContextMenu;
            ctxMenu.PlacementTarget = this.list;
            this.list.ContextMenu = ctxMenu;

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

            RoutedEventHandler delPrev = (s, e) => {
                var last = this.previous.Last();
                this.next.Insert(0, last);
                this.previous.RemoveAt(this.previous.Count - 1);
                this.menuPrev.IsEnabled = this.previous.Count > 1;
                (ctxMenu.Items[21] as MenuItem).IsEnabled = this.menuPrev.IsEnabled;
                this.menuNext.IsEnabled = this.next.Count > 0;

                this.Navigate(this.previous.Last(), true);
            };

            this.menuPrev.Click += delPrev;

            this.menuNext.Click += (s, e) => {
                var first = this.next[0];
                this.previous.Add(first);
                this.next.RemoveAt(0);
                this.menuPrev.IsEnabled = this.previous.Count > 1;
                (ctxMenu.Items[21] as MenuItem).IsEnabled = this.menuPrev.IsEnabled;
                this.menuNext.IsEnabled = this.next.Count > 0;

                this.Navigate(first, true);
            };

            RoutedEventHandler delUpOneLevel = (s, e) => {
                if (currentArchive == null) return;
                if (comboDirs.Count > 1) {
                    this.Navigate((FileSystemNode)comboDirs[comboDirs.Count - 2].SelectedItem, true);
                } else this.Navigate(currentArchive, true);
            };

            this.menuUplevel.Click += delUpOneLevel;

            RoutedEventHandler delRoot = (s, e) => {
                if (currentArchive == null) return;
                this.Navigate(currentArchive, true);
            };

            this.menuRootdir.Click += delRoot;

            this.hEdit.SubmenuOpened += (s, e) => {
                if (Clipboard.ContainsFileDropList() &&
                    this.currentArchive != null &&
                    this.currentNavigation != null) {
                    this.mnuPaste.IsEnabled = true;
                    (ctxMenu.Items[12] as MenuItem).IsEnabled = true;
                } else {
                    this.mnuPaste.IsEnabled = false;
                    (ctxMenu.Items[12] as MenuItem).IsEnabled = false;
                }
            };

            void updateMenuItems()
            {
                (ctxMenu.Items[0] as MenuItem).IsEnabled = false;
                (ctxMenu.Items[1] as MenuItem).IsEnabled = false;

                this.mnuDecompSelected.IsEnabled = false;
                (ctxMenu.Items[4] as MenuItem).IsEnabled = false;

                this.mnuDeleteSelection.IsEnabled = false;
                (ctxMenu.Items[10] as MenuItem).IsEnabled = false;
                this.mnuCopy.IsEnabled = false;
                (ctxMenu.Items[11] as MenuItem).IsEnabled = false;
                this.mnuPaste.IsEnabled = false;
                (ctxMenu.Items[12] as MenuItem).IsEnabled = false;
                this.mnuCut.IsEnabled = false;
                (ctxMenu.Items[13] as MenuItem).IsEnabled = false;
                this.mnuRename.IsEnabled = false;
                (ctxMenu.Items[14] as MenuItem).IsEnabled = false;

                (ctxMenu.Items[19] as MenuItem).IsEnabled = false;

                if (currentArchive == null) return;
                if (this.list.SelectedItem == null) return;
                if (this.list.SelectedItems.Count == 0) {
                    return;
                }

                if (this.list.SelectedItems.Count >= 1) {
                    this.mnuDecompSelected.IsEnabled = true;
                    (ctxMenu.Items[4] as MenuItem).IsEnabled = true;

                    this.mnuDeleteSelection.IsEnabled = true;
                    (ctxMenu.Items[10] as MenuItem).IsEnabled = true;
                    this.mnuCopy.IsEnabled = true;
                    (ctxMenu.Items[11] as MenuItem).IsEnabled = true;
                    if (Clipboard.ContainsFileDropList()) {
                        this.mnuPaste.IsEnabled = true;
                        (ctxMenu.Items[12] as MenuItem).IsEnabled = true;
                    }
                    this.mnuCut.IsEnabled = true;
                    (ctxMenu.Items[13] as MenuItem).IsEnabled = true;

                    this.mnuSelectExt.IsEnabled = true;
                    (ctxMenu.Items[19] as MenuItem).IsEnabled = true;
                }

                if (this.list.SelectedItems.Count == 1) {
                    (ctxMenu.Items[0] as MenuItem).IsEnabled = true;
                    (ctxMenu.Items[1] as MenuItem).IsEnabled = true;

                    this.mnuRename.IsEnabled = true;
                    (ctxMenu.Items[14] as MenuItem).IsEnabled = true;
                }
            }

            this.ctxMenu.Opened += (s, e) => {
                updateMenuItems();
            };

            this.list.SelectionChanged += (s, e) => {

                if (currentArchive == null) return;
                if (this.list.SelectedItems.Count == 0) {
                    return; 
                }

                if (this.list.SelectedItems.Count >= 1) {
                    this.mnuDecompSelected.IsEnabled = true;
                    (ctxMenu.Items[4] as MenuItem).IsEnabled = true;

                    this.mnuDeleteSelection.IsEnabled = true;
                    (ctxMenu.Items[10] as MenuItem).IsEnabled = true;
                    this.mnuCopy.IsEnabled = true;
                    (ctxMenu.Items[11] as MenuItem).IsEnabled = true;
                    if (Clipboard.ContainsFileDropList()) {
                        this.mnuPaste.IsEnabled = true;
                        (ctxMenu.Items[12] as MenuItem).IsEnabled = true;
                    }
                    this.mnuCut.IsEnabled = true;
                    (ctxMenu.Items[13] as MenuItem).IsEnabled = true;

                    this.mnuSelectExt.IsEnabled = true;
                    (ctxMenu.Items[19] as MenuItem).IsEnabled = true;
                }

                if (this.list.SelectedItems.Count == 1) {
                    (ctxMenu.Items[0] as MenuItem).IsEnabled = true;
                    (ctxMenu.Items[1] as MenuItem).IsEnabled = true;

                    this.mnuRename.IsEnabled = true;
                    (ctxMenu.Items[14] as MenuItem).IsEnabled = true;

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

            RoutedEventHandler delAbout = (s, e) => {
                (new About()).ShowDialog();
            };

            this.mnuAbout.Click += delAbout;

            RoutedEventHandler delDecompress = (s, e) => {
                if (currentArchive != null)
                    (new Extract(this.currentArchive)).ShowDialog();
            };

            this.mnuDecompress.Click += delDecompress;

            this.mnuAbout7z.Click += (s, e) => {
                System.Diagnostics.Process.Start("https://www.7-zip.org/");
            };

            this.mnuClearHistory.Click += (s, e) => {
                Properties.Settings.Default.History.Clear();
                Properties.Settings.Default.Save();
                this.mnuNoHistory.Visibility = Visibility.Visible;
                this.mnuRecent.Items.Clear();
                this.mnuRecent.Items.Add(this.mnuNoHistory);
                this.mnuRecent.Items.Add(new Separator());
                this.mnuRecent.Items.Add(this.mnuClearHistory);
            };

            if(Properties.Settings.Default.History == null) {
                Settings.Default.History = new System.Collections.Specialized.StringCollection();
                Settings.Default.Save();
            }
                
            foreach (var item in Properties.Settings.Default.History) {
                MenuItem mi = new MenuItem();
                mi.Header = item;
                mi.Click += (s, e) => {
                    this.openFile(item);
                };
                this.mnuRecent.Items.Insert(1, mi);
                this.mnuNoHistory.Visibility = Visibility.Collapsed;
            }

            bool isSPF()
            {
                if (currentArchive.Children.Any()) {
                    if (currentArchive.Children[0].IsFolder &&
                    currentArchive.Children[0].Name.EndsWith(":")) {
                        return true;
                    }
                }
                return false;
            }

            RoutedEventHandler delDecompressCurrent = (s, e) => {
                if (currentArchive == null) return;
                string destination = new FileInfo(currentArchive.FullName).Directory.FullName;
                string arguments = $"x \"{currentArchive.FullName}\"";
                bool _isSpf = false;

                if (currentArchive.Children.Any()) {
                    if (currentArchive.Children[0].IsFolder &&
                    currentArchive.Children[0].Name.EndsWith(":")) {
                        // spf
                        arguments += " -spf";
                        _isSpf = true;
                    }
                }
                if(!_isSpf)
                    arguments += $" -o\"{destination}\"";
                
                ActionProcess proc = new ActionProcess(
                    arguments,
                    $"Decompressing {currentArchive.Name} ...",
                    $"Decompressing archive file with command line options '{arguments}'"
                    );

                proc.Run();
            };

            this.mnuDecompCurrent.Click += delDecompressCurrent;

            RoutedEventHandler delReveal= (s, e) => {
                if (currentArchive == null) return;
                System.Diagnostics.Process.Start(
                    (new System.IO.FileInfo(this.currentArchive.FullName)).Directory.FullName);
            };

            this.mnuOpenInExplorer.Click += delReveal;

            RoutedEventHandler delDecompressSelected = (s, e) => {
                if (currentArchive == null) return;
                string destination = new FileInfo(currentArchive.FullName).Directory.FullName;

                // exclude working directory
                Guid taskGUID = Guid.NewGuid();
                string arguments = $"x \"{currentArchive.FullName}\"";
                string startup = System.Windows.Forms.Application.StartupPath + @"\";
                var workingDir = Directory.CreateDirectory(startup + @"working\" + taskGUID);

                if (isSPF())
                    arguments += " -spf";
                else
                    arguments += $" -o\"{destination}\"";

                using (FileStream listfile = new FileStream(workingDir.FullName + @"\selected.txt",
                       FileMode.OpenOrCreate)) {
                    using (StreamWriter writer = new StreamWriter(listfile, Encoding.UTF8)) {
                        foreach (Item item in this.list.SelectedItems) {
                            writer.WriteLine(item.FullName);
                        }

                        writer.Flush();
                    }
                }

                arguments += $" @\"{workingDir.FullName + @"\selected.txt"}\"";

                ActionProcess proc = new ActionProcess(
                    arguments,
                    $"Decompressing {currentArchive.Name} ...",
                    $"Decompressing archive file with command line options '{arguments}'"
                    );

                proc.Run();
                if (Directory.Exists(startup + @"working\" + taskGUID))
                    Directory.Delete(startup + @"working\" + taskGUID, true);
            };

            this.mnuDecompSelected.Click += delDecompressSelected;

            RoutedEventHandler delClose = (s, e) => {
                this.backToInitial();
            };

            this.mnuCloseArch.Click += delClose;

            RoutedEventHandler delDeselect = (s, e) => {
                this.list.UnselectAll();
            };

            RoutedEventHandler delSelectAll = (s, e) => {
                this.list.SelectAll();
            };

            RoutedEventHandler delReverseSelect = (s, e) => {
                List<object> objs = new List<object>();
                foreach(var x in list.Items) {
                    if (list.SelectedItems.Contains(x)) { }
                    else { objs.Add(x); }
                }

                this.list.UnselectAll();
                foreach (var x in objs) {
                    this.list.SelectedItems.Add(x);
                }
            };

            RoutedEventHandler delSelectExt = (s, e) => {
                string name = (this.list.SelectedItem as Item).Name;
                string ext = "." + name.Split('.').Last();
                if (!name.Contains(".")) ext = "";
                List<object> objs = new List<object>();
                foreach (Item x in list.Items) {
                    if (ext == "") {
                        if (!x.Name.Contains("."))
                            objs.Add(x);
                    }  else {
                        if (x.Name.EndsWith(ext))
                            objs.Add(x);
                    }
                }

                this.list.UnselectAll();
                foreach (var x in objs) {
                    this.list.SelectedItems.Add(x);
                }
            };

            this.mnuSelectAll.Click += delSelectAll;
            this.mnuSelectExt.Click += delSelectExt;
            this.mnuDeselect.Click += delDeselect;
            this.mnuReverse.Click += delReverseSelect;

            this.ctxMenu.Opened += (s, e) => {
                if (_initial_close_popup) {
                    this.ctxMenu.IsOpen = false;
                    _initial_close_popup = false;
                }
            };

            RoutedEventHandler delDeleteSelection = (s, e) => {
                if (currentArchive == null) return;
                string destination = new FileInfo(currentArchive.FullName).Directory.FullName;

                // exclude working directory
                Guid taskGUID = Guid.NewGuid();
                string arguments = $"d \"{currentArchive.FullName}\"";
                string startup = System.Windows.Forms.Application.StartupPath + @"\";
                var workingDir = Directory.CreateDirectory(startup + @"working\" + taskGUID);

                using (FileStream listfile = new FileStream(workingDir.FullName + @"\selected.txt",
                       FileMode.OpenOrCreate)) {
                    using (StreamWriter writer = new StreamWriter(listfile, Encoding.UTF8)) {
                        foreach (Item item in this.list.SelectedItems) {
                            writer.WriteLine(item.FullName);
                        }

                        writer.Flush();
                    }
                }

                arguments += $" @\"{workingDir.FullName + @"\selected.txt"}\"";

                ActionProcess proc = new ActionProcess(
                    arguments,
                    $"Deleting files from {currentArchive.Name} ...",
                    $"Deleting archive file with command line options '{arguments}'"
                    );

                proc.Run();

                // reopen and redirect
                string archivepath = currentArchive.FullName;
                string direc = getNavigationCascade();
                this.backToInitial();
                this.openFile(archivepath);
                this.Navigate(direc, this.currentArchive);

                if (Directory.Exists(startup + @"working\" + taskGUID))
                    Directory.Delete(startup + @"working\" + taskGUID, true);
            };

            void copyDirectory(DirectoryInfo src, DirectoryInfo working, string root = "")
            {
                foreach (var item in src.GetDirectories()) {
                    copyDirectory(item, working, src.Name + "\\");
                }

                foreach(var item in src.GetFiles()) {
                    copyFile(item, working, root + src.Name + "\\");
                }
            }

            void copyFile(FileInfo file, DirectoryInfo working, string root = "")
            {
                Directory.CreateDirectory(working.FullName + "\\" + root);
                File.Copy(file.FullName, working.FullName + "\\" + root + file.Name);
            }

            string getNavigationCascade()
            {
                if (currentNavigation == null) return "";
                if (this.currentNavigation is Archive a) {
                    return "";
                } else if (this.currentNavigation is FolderItem f) {
                    return f.FullName;
                } else throw new Exception("unexpected type of current navigation");
            }

            RoutedEventHandler delAdd = (s, e) => {
                if (currentArchive == null) return;
                if (currentNavigation == null) throw new Exception("current navigation not set.");

                // copy directories and files
                Guid taskGUID = Guid.NewGuid();
                string arguments = $"a \"{currentArchive.FullName}\"";
                string startup = System.Windows.Forms.Application.StartupPath + @"\";
                var workingDir = Directory.CreateDirectory(startup + @"working\" + taskGUID);

                FileSelector selector = new FileSelector();
                if(selector.ShowDialog() == true) {
                    foreach (var item in selector.SelectedDirectories) {
                        copyDirectory(item, new DirectoryInfo(
                            workingDir.FullName + "\\" + 
                            getNavigationCascade().Replace("/","\\") ));
                    }

                    foreach(var item in selector.SelectedFiles) {
                        copyFile(item, new DirectoryInfo(
                            workingDir.FullName + "\\" +
                            getNavigationCascade().Replace("/", "\\")));
                    }

                    using (FileStream listfile = new FileStream(workingDir.FullName + @"\selected.txt",
                       FileMode.OpenOrCreate)) {
                        using (StreamWriter writer = new StreamWriter(listfile, Encoding.UTF8)) {
                            foreach (var item in selector.SelectedDirectories) {
                                if (getNavigationCascade() != "")
                                    writer.WriteLine($"{getNavigationCascade().Replace("/", "\\") + "\\" + item.Name}\\");
                                else writer.WriteLine($"{item.Name}\\");
                            }

                            foreach (var item in selector.SelectedFiles) {
                                if (getNavigationCascade() != "")
                                    writer.WriteLine($"{getNavigationCascade().Replace("/", "\\") + "\\" + item.Name}");
                                else writer.WriteLine($"{item.Name}");
                            }

                            writer.Flush();
                        }
                    }

                    arguments += $" @\"{workingDir.FullName + @"\selected.txt"}\"";
                    ActionProcess proc = new ActionProcess(
                        arguments,
                        $"Updating files to {currentArchive.Name} ...",
                        $"Updating archive file with command line options '{arguments}'",
                        specifiedId: taskGUID
                    );

                    proc.Run();

                    // reopen and redirect
                    string archivepath = currentArchive.FullName;
                    string direc = getNavigationCascade();
                    this.backToInitial();
                    this.openFile(archivepath);
                    this.Navigate(direc, this.currentArchive);
                }

                if (Directory.Exists(startup + @"working\" + taskGUID))
                    Directory.Delete(startup + @"working\" + taskGUID, true);
            };

            this.mnuDeleteSelection.Click += delDeleteSelection;
            this.mnuAppend.Click += delAdd;

            RoutedEventHandler delCopy = (s, e) => {
                if (currentArchive == null) return;
                string destination = new FileInfo(currentArchive.FullName).Directory.FullName;

                // exclude working directory
                Guid taskGUID = Guid.NewGuid();
                string arguments = $"x \"{currentArchive.FullName}\"";
                string startup = System.Windows.Forms.Application.StartupPath + @"\";
                var workingDir = Directory.CreateDirectory(startup + @"working\" + taskGUID);

                // specify the working directory whether it is spf or not.
                arguments += $" -o\"{workingDir.FullName}\"";
                System.Collections.Specialized.StringCollection clipboardStrings = new System.Collections.Specialized.StringCollection();
                using (FileStream listfile = new FileStream(workingDir.FullName + @"\selected.txt",
                       FileMode.OpenOrCreate)) {
                    using (StreamWriter writer = new StreamWriter(listfile, Encoding.UTF8)) {

                        string basepath = workingDir.FullName + "\\" + getNavigationCascade().Replace("/", "\\") + "\\";
                        if (isSPF())
                            basepath = basepath.Replace(":", "_");

                        foreach (Item item in this.list.SelectedItems) {
                            writer.WriteLine(item.FullName);
                            clipboardStrings.Add(basepath + item.Name);
                        }

                        writer.Flush();
                    }
                }

                arguments += $" @\"{workingDir.FullName + @"\selected.txt"}\"";

                ActionProcess proc = new ActionProcess(
                    arguments,
                    $"Decompressing {currentArchive.Name} ...",
                    $"Decompressing archive file with command line options '{arguments}'"
                    );

                proc.Run();

                // set to clipboard
                Clipboard.SetFileDropList(clipboardStrings);

                // delay deletion of the working directory
                this.delayedWorkingDir.Add(taskGUID);
            };

            RoutedEventHandler delCut = (s, e) => {
                delCopy(s, e);
                delDeleteSelection(s, e);
            };

            RoutedEventHandler delPaste = (s, e) => {
                // is actually a modified version of delAdd.
                if (currentArchive == null) return;
                if (currentNavigation == null) throw new Exception("current navigation not set.");

                // copy directories and files
                Guid taskGUID = Guid.NewGuid();
                string arguments = $"a \"{currentArchive.FullName}\"";
                string startup = System.Windows.Forms.Application.StartupPath + @"\";
                var workingDir = Directory.CreateDirectory(startup + @"working\" + taskGUID);

                if (Clipboard.ContainsFileDropList()) {
                    foreach (var item in Clipboard.GetFileDropList()) {
                        if (Directory.Exists(item)) {
                            copyDirectory(new DirectoryInfo(item), new DirectoryInfo(
                                workingDir.FullName + "\\" +
                                getNavigationCascade().Replace("/", "\\")));
                        } else if (File.Exists(item)) {
                            copyFile(new FileInfo(item), new DirectoryInfo(
                                workingDir.FullName + "\\" +
                                getNavigationCascade().Replace("/", "\\")));
                        } else {
                            MessageBox.Show("The path in the clipboard is invalid, probably the source is moved or deleted: \n" + item);
                        }
                    }

                    using (FileStream listfile = new FileStream(workingDir.FullName + @"\selected.txt",
                       FileMode.OpenOrCreate)) {
                        using (StreamWriter writer = new StreamWriter(listfile, Encoding.UTF8)) {

                            foreach (var item in Clipboard.GetFileDropList()) {
                                if (Directory.Exists(item)) {
                                    if (getNavigationCascade() != "")
                                        writer.WriteLine($"{getNavigationCascade().Replace("/", "\\") + "\\" + new DirectoryInfo(item).Name}\\");
                                    else writer.WriteLine($"{new DirectoryInfo(item).Name}\\");
                                } else if (File.Exists(item)) {
                                    if (getNavigationCascade() != "")
                                        writer.WriteLine($"{getNavigationCascade().Replace("/", "\\") + "\\" + new FileInfo(item).Name}");
                                    else writer.WriteLine($"{new FileInfo(item).Name}");
                                } 
                            }

                            writer.Flush();
                        }
                    }

                    arguments += $" @\"{workingDir.FullName + @"\selected.txt"}\"";
                    ActionProcess proc = new ActionProcess(
                        arguments,
                        $"Updating files to {currentArchive.Name} ...",
                        $"Updating archive file with command line options '{arguments}'",
                        specifiedId: taskGUID
                    );

                    proc.Run();

                    // reopen and redirect
                    string archivepath = currentArchive.FullName;
                    string direc = getNavigationCascade();
                    this.backToInitial();
                    this.openFile(archivepath);
                    this.Navigate(direc, this.currentArchive);
                }

                if (Directory.Exists(startup + @"working\" + taskGUID))
                    Directory.Delete(startup + @"working\" + taskGUID, true);
            };

            this.mnuCopy.Click += delCopy;
            this.mnuCut.Click += delCut;
            this.mnuPaste.Click += delPaste;

            RoutedEventHandler delRename = (s, e) => {
                if (currentArchive == null) return;

                string arguments = $"rn \"{currentArchive.FullName}\"";
                string startup = System.Windows.Forms.Application.StartupPath + @"\";

                string basepath = getNavigationCascade();
                string renfile = (this.list.SelectedItem as Item).Name;
                if (basepath != "") basepath = basepath + "/";

                Rename rn = new Rename(renfile);
                if (rn.ShowDialog() == true) {
                    arguments += $" \"{basepath}{renfile}\" \"{basepath}{rn.NewName}\"";

                    ActionProcess proc = new ActionProcess(
                        arguments,
                        $"Renaming ...",
                        $"Renaming {renfile} with {rn.NewName}"
                        );

                    proc.Run();

                    // reopen and redirect
                    string archivepath = currentArchive.FullName;
                    string direc = getNavigationCascade();
                    this.backToInitial();
                    this.openFile(archivepath);
                    this.Navigate(direc, this.currentArchive);
                }
            };

            this.mnuRename.Click += delRename;

            ctxMenu.IsOpen = true;
            ctxMenu.Items.Add((new MenuItem { Header = "Open" })); // [0]
            ctxMenu.Items.Add((new MenuItem { Header = "Open with", IsEnabled = false }));
            ctxMenu.Items.Add((new Separator()));
            ctxMenu.Items.Add((new MenuItem { Header = "Decompress all to current path", InputGestureText = "Ctrl + D" }).attachEvent(delDecompressCurrent)); // [3]
            ctxMenu.Items.Add((new MenuItem { Header = "Decompress selected items", InputGestureText = "Ctrl + Shift + D" }).attachEvent(delDecompressSelected));
            ctxMenu.Items.Add((new MenuItem { Header = "Decompress ..." }).attachEvent(delDecompress));
            ctxMenu.Items.Add((new Separator()));
            ctxMenu.Items.Add((new MenuItem { Header = "Reveal in file explorer ...", InputGestureText = "Ctrl + D" }).attachEvent(delReveal));
            ctxMenu.Items.Add((new MenuItem { Header = "Append files into archive ...", InputGestureText = "Ctrl + P" }).attachEvent(delAdd));
            ctxMenu.Items.Add((new Separator()));
            ctxMenu.Items.Add((new MenuItem { Header = "Delete", InputGestureText = "Del" }).attachEvent(delDeleteSelection));
            ctxMenu.Items.Add((new MenuItem { Header = "Copy", InputGestureText = "Ctrl + C" }).attachEvent(delCopy));
            ctxMenu.Items.Add((new MenuItem { Header = "Paste", InputGestureText = "Ctrl + V" }).attachEvent(delPaste));
            ctxMenu.Items.Add((new MenuItem { Header = "Cut", InputGestureText = "Ctrl + D" }).attachEvent(delCut));
            ctxMenu.Items.Add((new MenuItem { Header = "Rename ...", InputGestureText = "F2" }).attachEvent(delRename));
            ctxMenu.Items.Add((new Separator()));
            ctxMenu.Items.Add((new MenuItem { Header = "Unselect all", InputGestureText = "Ctrl + Shift + A" }).attachEvent(delDeselect));
            ctxMenu.Items.Add((new MenuItem { Header = "Select all", InputGestureText = "Ctrl + A" }).attachEvent(delSelectAll));
            ctxMenu.Items.Add((new MenuItem { Header = "Reverse selection", InputGestureText = "Ctrl + R" }).attachEvent(delReverseSelect));
            ctxMenu.Items.Add((new MenuItem { Header = "Select by extension"}).attachEvent(delSelectExt));
            ctxMenu.Items.Add((new Separator()));
            ctxMenu.Items.Add((new MenuItem { Header = "Previous" }).attachEvent(delPrev));
            ctxMenu.Items.Add((new MenuItem { Header = "Up one level", InputGestureText = "Backspace" }).attachEvent(delUpOneLevel));

            closeFile();

            this.Closed += (s, e) => {
                foreach (var taskGUID in this.delayedWorkingDir) {
                    string startup = System.Windows.Forms.Application.StartupPath + @"\";
                    if (Directory.Exists(startup + @"working\" + taskGUID))
                        Directory.Delete(startup + @"working\" + taskGUID, true);
                }
            };

            registerInputGesture("UpOneLevel",
                () => { updateMenuItems(); return this.menuUplevel.IsEnabled; },
                delUpOneLevel);
            registerInputGesture("Root",
                () => { updateMenuItems(); return this.menuRootdir.IsEnabled; },
                delRoot);

            registerInputGesture("Open",
                () => { updateMenuItems(); return this.menuOpen.IsEnabled; },
                menuOpen_Click);
            registerInputGesture("Create",
                () => { updateMenuItems(); return this.menuCreate.IsEnabled; },
                menuCreate_Click);
            registerInputGesture("DecompressSelected",
                () => { updateMenuItems(); return this.mnuDecompSelected.IsEnabled; },
                delDecompressSelected);
            registerInputGesture("Decompress",
                () => { updateMenuItems(); return this.mnuDecompCurrent.IsEnabled; },
                delDecompressCurrent);

            registerInputGesture("Append",
                () => { updateMenuItems(); return this.mnuAppend.IsEnabled; },
                delAdd);
            registerInputGesture("Cut",
                () => { updateMenuItems(); return this.mnuCut.IsEnabled; },
                delCut);
            registerInputGesture("Copy",
                () => { updateMenuItems(); return this.mnuCopy.IsEnabled; },
                delCopy);
            registerInputGesture("Paste",
                () => { updateMenuItems(); return this.mnuPaste.IsEnabled; },
                delPaste);
            registerInputGesture("Rename",
                () => { updateMenuItems(); return this.mnuRename.IsEnabled; },
                delRename);
            registerInputGesture("Delete",
                () => { updateMenuItems(); return this.mnuDeleteSelection.IsEnabled; },
                delDeleteSelection);

            registerInputGesture("Select",
                () => { updateMenuItems(); return this.mnuSelectAll.IsEnabled; },
                delSelectAll);
            registerInputGesture("Unselect",
                () => { updateMenuItems(); return this.mnuDeselect.IsEnabled; },
                delDeselect);
            registerInputGesture("Reverse",
                () => { updateMenuItems(); return this.mnuReverse.IsEnabled; },
                delReverseSelect);

            registerInputGesture(verb: "Find",
                () => { return this.currentArchive != null; },
                (s, e) => { MessageBox.Show("Hello, world!"); });
        }

        private void registerInputGesture(string verb, Func<bool> predicate, RoutedEventHandler handler)
        {
            CommandBinding cb = this.Resources["cb" + verb] as CommandBinding;
            this.CommandBindings.Add(cb);
            cb.CanExecute += (s, e) => {
                e.CanExecute = predicate();
            };

            cb.Executed += (s, e) => {
                handler(s, null);
            };
        }
        
        // black magic :)
        bool _initial_close_popup = true;

        private void backToInitial()
        {
            if (this.currentArchive != null)
                closeFile();
        }

        private static object deepCopy(object _object)
        {
            Type T = _object.GetType();
            object o = Activator.CreateInstance(T);
            PropertyInfo[] PI = T.GetProperties();
            for (int i = 0; i < PI.Length; i++) {
                PropertyInfo P = PI[i];
                P.SetValue(o, P.GetValue(_object));
            }
            return o;
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

        private void closeFile()
        {
            this.currentArchive = null;
            this.currentNavigation = null;
            this.dirCombo.Content = "(Empty)";

            this.previous.Clear();
            this.next.Clear();
            this.menuRootdir.IsEnabled = false;
            this.menuUplevel.IsEnabled = false;
            (ctxMenu.Items[22] as MenuItem).IsEnabled = false;
            this.splashScreen.Visibility = Visibility.Visible;
            this.gridList.Visibility = Visibility.Hidden;
            this.gridDetails.Visibility = Visibility.Hidden;

            this.mnuDecompress.IsEnabled = true;

            this.imgIcon.Source = this.Icon;
            this.lblDesc.Text = "";
            this.tArchiveType.Text = "";
            this.sArchiveType.Visibility = Visibility.Collapsed;
            this.tPhysicalSize.Text = "";
            this.sPhysicalSize.Visibility = Visibility.Collapsed;
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

            this.lblTitle.Text = "Archiver";
            this.Title = "Archiver";

            (ctxMenu.Items[0] as MenuItem).IsEnabled = false;
            (ctxMenu.Items[1] as MenuItem).IsEnabled = false;

            this.mnuDecompCurrent.IsEnabled = false;
            (ctxMenu.Items[3] as MenuItem).IsEnabled = false;
            this.mnuDecompSelected.IsEnabled = false;
            (ctxMenu.Items[4] as MenuItem).IsEnabled = false;
            this.mnuDecompress.IsEnabled = false;
            (ctxMenu.Items[5] as MenuItem).IsEnabled = false;

            this.mnuOpenInExplorer.IsEnabled = false;
            (ctxMenu.Items[7] as MenuItem).IsEnabled = false;
            this.mnuAppend.IsEnabled = false;
            (ctxMenu.Items[8] as MenuItem).IsEnabled = false;
            this.mnuDeleteSelection.IsEnabled = false;
            (ctxMenu.Items[10] as MenuItem).IsEnabled = false;
            this.mnuCopy.IsEnabled = false;
            (ctxMenu.Items[11] as MenuItem).IsEnabled = false;
            this.mnuPaste.IsEnabled = false;
            (ctxMenu.Items[12] as MenuItem).IsEnabled = false;
            this.mnuCut.IsEnabled = false;
            (ctxMenu.Items[13] as MenuItem).IsEnabled = false;
            this.mnuRename.IsEnabled = false;
            (ctxMenu.Items[14] as MenuItem).IsEnabled = false;

            this.mnuDeselect.IsEnabled = false;
            (ctxMenu.Items[16] as MenuItem).IsEnabled = false;
            this.mnuSelectAll.IsEnabled = false;
            (ctxMenu.Items[17] as MenuItem).IsEnabled = false;
            this.mnuReverse.IsEnabled = false;
            (ctxMenu.Items[18] as MenuItem).IsEnabled = false;
            this.mnuSelectExt.IsEnabled = false;
            (ctxMenu.Items[19] as MenuItem).IsEnabled = false;

            this.menuPrev.IsEnabled = false;
            (ctxMenu.Items[21] as MenuItem).IsEnabled = false;
            this.menuUplevel.IsEnabled = false;
            (ctxMenu.Items[22] as MenuItem).IsEnabled = false;

            this.mnuCloseArch.IsEnabled = false;
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
                        this.Title = System.IO.Path.GetFileName(path);

                        // now fininshes current archive parsing.
                        Navigate(currentArchive);

                        {
                            this.imgIcon.Source = currentArchive.Icon;
                            currentNavigation = currentArchive;
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
                        (ctxMenu.Items[22] as MenuItem).IsEnabled = true;
                        this.splashScreen.Visibility = Visibility.Hidden;
                        this.gridList.Visibility = Visibility.Visible;
                        this.gridDetails.Visibility = Visibility.Visible;

                        this.mnuAppend.IsEnabled = true;
                        (ctxMenu.Items[8] as MenuItem).IsEnabled = true;
                        this.mnuDecompress.IsEnabled = true;
                        (ctxMenu.Items[5] as MenuItem).IsEnabled = true;
                        this.mnuDecompCurrent.IsEnabled = true;
                        (ctxMenu.Items[3] as MenuItem).IsEnabled = true;

                        this.mnuDeselect.IsEnabled = true;
                        (ctxMenu.Items[16] as MenuItem).IsEnabled = true;
                        this.mnuSelectAll.IsEnabled = true;
                        (ctxMenu.Items[17] as MenuItem).IsEnabled = true;
                        this.mnuReverse.IsEnabled = true;
                        (ctxMenu.Items[18] as MenuItem).IsEnabled = true;

                        this.mnuOpenInExplorer.IsEnabled = true;
                        (ctxMenu.Items[7] as MenuItem).IsEnabled = true;
                        this.mnuCloseArch.IsEnabled = true;
                    }));
                };

                Loader loader = new Loader(worker);
                loader.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                loader.ResizeMode = ResizeMode.NoResize;
                loader.ShowDialog();

                if(!Settings.Default.History.Contains(path)) {
                    MenuItem mi = new MenuItem();
                    mi.Header = path;
                    mi.Click += (s, e) => {
                        this.openFile(path);
                    };
                    this.mnuRecent.Items.Insert(1, mi);
                    this.mnuNoHistory.Visibility = Visibility.Collapsed;
                    Settings.Default.History.Add(path);
                    Settings.Default.Save();
                }
            }
        }

        FileSystemNode currentNavigation;
        bool comboSuppress = false;
        private void Navigate(FileSystemNode directory, bool suppress = false)
        {
            if (currentArchive == null) return;

            if (!suppress) {
                this.previous.Add(directory);
                this.menuPrev.IsEnabled = this.previous.Count > 1;
                (ctxMenu.Items[21] as MenuItem).IsEnabled = this.menuPrev.IsEnabled;
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
                currentNavigation = archive;

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
                currentNavigation = folder;

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

                if (found != null) { 
                    Navigate(found);
                } else Navigate(parent);

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


        public static MenuItem attachEvent(this MenuItem item, RoutedEventHandler handle)
        {
            item.Click += handle;
            return item;
        }
    }
}
