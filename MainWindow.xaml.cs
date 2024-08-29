using Archiver.Archive;
using Archiver.Properties;
using Microsoft.Win32;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Vanara.PInvoke.Shell32;

namespace Archiver
{
    public partial class MainWindow : Window
    {
        public ContextMenu ctxMenu;

        private bool isModelDialogOpened = false;

        public MainWindow()
        {
            InitializeComponent();

            // hide property stacks by default
            resetPropertyStack();

            this.DataContext = this;
            this.breadcrumb.DataContext = null;

            this.ctxMenu = this.Resources["ctxMenu"] as ContextMenu ??
                throw new Exception("internal exception 01");
            this.ctxMenu.PlacementTarget = this.list;
            this.list.ContextMenu = ctxMenu;
            this.ctxMenu.DataContext = this;

            this.menu.DataContext = this;

            string _env = AppDomain.CurrentDomain.BaseDirectory + @"\";
            if (!Directory.Exists(_env + @"working"))
                Directory.CreateDirectory(_env + @"\working\");
            if (!Directory.Exists(_env + @"temp"))
                Directory.CreateDirectory(_env + @"\temp\");

            ImageSourceConverter isc = new ImageSourceConverter();

            this.StateChanged += (s, e) =>
            {
                if (this.WindowState == WindowState.Maximized)
                    windowGrid.Margin = new Thickness(8);
                else
                    windowGrid.Margin = new Thickness(0);
            };

            this.btnCancel.Click += (s, e) => { this.CancelRequest?.Invoke(s, e); };
            this.btnProceed.Click += (s, e) => { this.ProceedRequest?.Invoke(s, e); };

            #region Navigation

            this.list.MouseDoubleClick += (s, e) =>
            {
                if (this.currentArchive == null) return;
                if (this.list.SelectedItem == null) return;

                if (this.list.SelectedItem is FolderItem folder)
                {
                    navigate(folder);
                }
            };

            this.list.SelectionChanged += (s, e) =>
            {
                // update details panel
                if (this.list.SelectedItem is FileSystemNode node)
                    updateProperties(node);
            };

            this.breadcrumb.SelectionChanged += (s, e) =>
            {
                if (this.currentArchive == null) return;

                if (this.breadcrumb.SelectedItem is FileSystemNode node)
                    navigate(node, true);
                else if (this.breadcrumb.SelectedItem == this.bcRoot)
                    navigate(currentArchive, true);
            };

            #endregion

            #region File Menu

            this.OpenArchive = registerCommand(
                "Open from file ...", "open.arch",
                new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl + O"),
                (o, args) =>
                {
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.Multiselect = false;
                    // dialog.AddToRecent = Settings.Default.OpenFileDialogAddToRecent;

                    if (dialog.ShowDialog() ?? false)
                    {
                        openFile(isc, dialog.FileName);
                    }
                },
                (o, args) =>
                {
                    return !isModelDialogOpened;
                }
            );

            this.Create = registerCommand("Create archive ...", "create",
                new KeyGesture(Key.N, ModifierKeys.Control, "Ctrl + N"),
                (s, e) => 
                {
                    isModelDialogOpened = true;
                    var tempVisibility = this.splashScreen.Visibility;
                    var tempTitle = this.lblTitle.Text;
                    var tempWindowTitle = this.Title;

                    this.resetStatusBar();
                    CompressPage page = new CompressPage(this);
                    this.statusBar.Visibility = Visibility.Visible;
                    this.splashScreen.Visibility = Visibility.Visible;
                    this.defaultSplash.Visibility = Visibility.Hidden;
                    this.splashScreen.Children.Add(page);

                    this.lblTitle.Text = "Create archive";
                    this.Title = "Create archive";

                    page.JobFinished += (s, e) =>
                    {
                        this.splashScreen.Children.Remove(page);
                        this.defaultSplash.Visibility = Visibility.Visible;
                        this.splashScreen.Visibility = tempVisibility;
                        this.resetStatusBar();
                        this.statusBar.Visibility = Visibility.Collapsed;

                        this.lblTitle.Text = tempTitle;
                        this.Title = tempWindowTitle;
                        isModelDialogOpened = false;
                    };
                },
                (s, e) => { return !isModelDialogOpened; }
            );

            this.Decompress = registerCommand("Extract archive ...", "decompress",
                new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + E"),
                (s, e) =>
                {
                    isModelDialogOpened = true;
                    var tempVisibility = this.splashScreen.Visibility; 
                    var tempTitle = this.lblTitle.Text;
                    var tempWindowTitle = this.Title;

                    this.resetStatusBar();
                    ExtractPage page = new ExtractPage(this, this.currentArchive ?? throw new Exception("Unexpected"));
                    this.statusBar.Visibility = Visibility.Visible;
                    this.splashScreen.Visibility = Visibility.Visible;
                    this.defaultSplash.Visibility = Visibility.Hidden;
                    this.splashScreen.Children.Add(page);

                    this.lblTitle.Text = "Extract archive";
                    this.Title = "Extract archive";

                    page.JobFinished += (s, e) =>
                    {
                        this.splashScreen.Children.Remove(page);
                        this.defaultSplash.Visibility = Visibility.Visible;
                        this.splashScreen.Visibility = tempVisibility;
                        this.resetStatusBar();
                        this.statusBar.Visibility = Visibility.Collapsed;
                        
                        this.lblTitle.Text = tempTitle;
                        this.Title = tempWindowTitle;
                        isModelDialogOpened = false;
                    };
                },
                (s, e) => 
                { 
                    return !isModelDialogOpened && 
                           this.IsArchiveOpened; 
                }
            );

            this.DecompressToCurrent = registerCommand("Decompress to current directory", "decompress.current",
                new KeyGesture(Key.E, ModifierKeys.Control, "Ctrl + E"),
                (s, e) =>
                {
                    isModelDialogOpened = true;
                    var tempVisibility = this.splashScreen.Visibility;
                    var tempTitle = this.lblTitle.Text;
                    var tempWindowTitle = this.Title;

                    this.resetStatusBar();
                    ExtractPage page = new ExtractPage(this, this.currentArchive ?? throw new Exception("Unexpected"), autoextract: true);
                    this.statusBar.Visibility = Visibility.Visible;
                    this.splashScreen.Visibility = Visibility.Visible;
                    this.defaultSplash.Visibility = Visibility.Hidden;
                    this.splashScreen.Children.Add(page);

                    this.lblTitle.Text = "Extract archive";
                    this.Title = "Extract archive";

                    page.JobFinished += (s, e) =>
                    {
                        this.splashScreen.Children.Remove(page);
                        this.defaultSplash.Visibility = Visibility.Visible;
                        this.splashScreen.Visibility = tempVisibility;
                        this.resetStatusBar();
                        this.statusBar.Visibility = Visibility.Collapsed;

                        this.lblTitle.Text = tempTitle;
                        this.Title = tempWindowTitle;
                        isModelDialogOpened = false;
                    };
                },
                (s, e) =>
                {
                    return !isModelDialogOpened &&
                           this.IsArchiveOpened;
                }
            );

            this.Reveal = registerCommand("Reveal in Windows Explorer ...", "reveal", null,
                (s, e) =>
                {
                    if (currentArchive == null) return;
                    DirectoryInfo? dinfo = new System.IO.FileInfo(this.currentArchive.FullName).Directory;
                    if (dinfo != null)
                        System.Diagnostics.Process.Start(new ProcessStartInfo(dinfo.FullName) { UseShellExecute = true });
                },
                (s, e) =>
                {
                    return this.IsArchiveOpened;
                }
            );

            this.Quit = registerCommand("Quit application", "quit", 
                new KeyGesture(Key.F4, ModifierKeys.Alt, "Alt + F4"),
                (s, e) =>
                {
                    Application.Current.Shutdown();
                },
                (s, e) =>
                {
                    return true;
                }
            );

            this.mnuClearHistory.Click += (s, e) => {
                Properties.Settings.Default.History.Clear();
                Properties.Settings.Default.Save();
                this.mnuNoHistory.Visibility = Visibility.Visible;
                this.mnuRecent.Items.Clear();
                this.mnuRecent.Items.Add(this.mnuNoHistory);
                this.mnuRecent.Items.Add(new Separator());
                this.mnuRecent.Items.Add(this.mnuClearHistory);
            };

            if (Properties.Settings.Default.History == null)
            {
                Settings.Default.History = new System.Collections.Specialized.StringCollection();
                Settings.Default.Save();
            }

            foreach (var item in Properties.Settings.Default.History)
            {
                MenuItem mi = new MenuItem();
                if (item == null) continue;
                FileInfo info = new FileInfo(item);
                if (!info.Exists) continue;
                mi.Header = info.Name;
                mi.InputGestureText = info.Directory?.FullName ?? "";
                mi.Click += (s, e) => {
                    this.openFile(isc, item);
                };
                this.mnuRecent.Items.Insert(1, mi);
                this.mnuNoHistory.Visibility = Visibility.Collapsed;
            }

            #endregion

            #region View Menu

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

            Action<object, ExecutedRoutedEventArgs> delPrev = (s, e) => {
                var last = this.previous.Last();
                this.next.Insert(0, last);
                this.previous.RemoveAt(this.previous.Count - 1);

                this.navigate(this.previous.Last(), true);
            };

            Action<object, ExecutedRoutedEventArgs> delNext = (s, e) => {
                var first = this.next[0];
                this.previous.Add(first);
                this.next.RemoveAt(0);

                this.navigate(first, true);
            };

            Action<object, ExecutedRoutedEventArgs> delUpOneLevel = (s, e) => {
                if (currentArchive == null) return;
                if (this.current?.Parent != null)
                {
                    this.navigate(this.current.Parent, true);
                } else this.navigate(currentArchive, true);
            };

            Action<object, ExecutedRoutedEventArgs> delRoot = (s, e) => {
                if (currentArchive == null) return;
                this.navigate(currentArchive, true);
            };

            this.Previous = this.registerCommand("Previous", "previous", null,
                delPrev, (s, e) => { return this.IsArchiveOpened && this.previous.Count > 1; });
            this.Next = this.registerCommand("Next", "next", null,
                delNext, (s, e) => { return this.IsArchiveOpened && this.next.Any(); });
            this.LevelUp = this.registerCommand("Up one level", "up.level", new KeyGesture(Key.Back, ModifierKeys.None, "Backspace"),
                delUpOneLevel, (s, e) => { return this.IsArchiveOpened && this.current?.Parent != null; });
            this.Root = this.registerCommand("Archive root directory", "root.dir", new KeyGesture(Key.Decimal, ModifierKeys.Control, "Ctrl + ."),
                delRoot, (s, e) => { return this.IsArchiveOpened; });

            this.btnNext.Command = this.Next;
            this.btnPrev.Command = this.Previous;

            #endregion

            #region Help Menu

            this.About7Z = registerCommand("About 7-Zip ...", "about.7z", null,
                (s, e) => { Process.Start(new ProcessStartInfo("https://www.7-zip.org/") { UseShellExecute = true }); },
                (s, e) => { return true; });

            this.About = registerCommand("About Archiver ...", "about", null,
                (s, e) => { (new About()).ShowDialog(); },
                (s, e) => { return true; });

            #endregion

            this.Loaded += (s, e) =>
            {
                string[] pargs = Environment.GetCommandLineArgs();
                if (pargs.Count() == 2)
                {
                    string fname = pargs[1];
                    if (File.Exists(fname))
                    {
                        this.openFile(isc, fname);
                    }
                }
            };
        }

        private void openFile(ImageSourceConverter isc, string fname)
        {
            Archiver.Archive.SevenZipExtractor? extractor = null;

            try
            {
                extractor = new Archiver.Archive.SevenZipExtractor(fname);
                var properties = extractor.ArchiveProperties;
            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            if (extractor == null) return;
            if (extractor.HasExceptions) return;

            Arch arch = new Arch();
            arch.Format = extractor.Format.ToString();
            arch.FullName = fname;
            arch.Name = new FileInfo(fname).Name;
            arch.PackedSize = Convert.ToUInt64(extractor.PackedSize);
            arch.Icon = isc.ConvertFrom("pack://siteoforigin:,,,/resources/archive.ico") as ImageSource;

            foreach (var fileData in extractor.ArchiveFileData)
            {
                Item? item = null;
                if (!fileData.IsDirectory)
                    item = new FileItem(fileData);
                else item = new FolderItem(fileData);

                if (item == null) continue;

                string[] fullpath = item.FullName.Split('\\');
                item.Name = fullpath.Last();
                FolderItem? parent = null;
                string fullCascadeName = "";

                if (fullpath.Count() > 1)
                {
                    string cascadeName = fullpath[0];
                    fullCascadeName = fullCascadeName + cascadeName;
                    parent = (FolderItem?)arch.Children.Find((x) =>
                    {
                        return (x.Name == cascadeName && x.IsFolder);
                    });

                    if (parent == null)
                    {
                        parent = new FolderItem()
                        {
                            Name = cascadeName,
                            FullName = fullCascadeName,
                            IsFolder = true,
                            Icon = isc.ConvertFrom("pack://siteoforigin:,,,/resources/folder.ico") as ImageSource
                        };
                        arch.Children.Add(parent);
                        parent.Parent = arch;
                    }

                    for (int cascade = 1; cascade < fullpath.Length - 1; cascade++)
                    {
                        cascadeName = fullpath[cascade];
                        fullCascadeName = fullCascadeName + "\\" + cascadeName;
                        FolderItem? fi = (FolderItem?)parent.Children.Find((x) =>
                        {
                            return x.Name == cascadeName && x.IsFolder;
                        });

                        if (fi == null)
                        {
                            fi = new FolderItem()
                            {
                                Name = cascadeName,
                                FullName = fullCascadeName,
                                IsFolder = true,
                                Icon = isc.ConvertFrom("pack://siteoforigin:,,,/resources/folder.ico") as ImageSource
                            };
                            parent.Children.Add(fi);
                            fi.Parent = parent;
                        }

                        parent = fi;
                    }

                    if (item.IsFolder)
                    {
                        FolderItem? found = (FolderItem?)parent.Children.Find((x) =>
                        {
                            return (x.Name == item.Name && x.IsFolder);
                        });

                        if (found != null)
                        {
                            var folder = item as FolderItem;
                            if (folder != null) folder.Children = found.Children;
                            parent.Children.Remove(found);
                        }
                    }

                    parent.Children.Add(item);
                    item.Parent = parent;
                    continue;
                }

                if (item.IsFolder)
                {
                    FolderItem? found = (FolderItem?)arch.Children.Find((x) =>
                    {
                        return (x.Name == item.Name && x.IsFolder);
                    });

                    if (found != null)
                    {
                        var folder = item as FolderItem;
                        if (folder != null) folder.Children = found.Children;
                        arch.Children.Remove(found);
                    }
                }

                arch.Children.Add(item);
                item.Parent = arch;
            }

            this.currentArchive = arch;
            this.lblTitle.Text = "Archiver · " + System.IO.Path.GetFileName(arch.Name);
            this.Title = arch.Name;
            extractor.Dispose(); // free the file handle.

            this.previous.Clear();
            this.next.Clear();

            string contains = "";
            foreach (var his in Properties.Settings.Default.History)
                if ((his ?? "").ToLower().Trim() == arch.FullName.ToLower().Trim())
                    contains = his ?? "";

            if (!string.IsNullOrEmpty(contains))
                Properties.Settings.Default.History.Remove(contains);

            Properties.Settings.Default.History.Add(arch.FullName);
            Properties.Settings.Default.Save();

            this.bcRoot.ItemsSource = arch;
            this.navigate(arch);
            this.updateProperties(arch);
            this.splashScreen.Visibility = Visibility.Hidden;
        }

        private void navigate(FileSystemNode directory, bool suppress = false)
        {
            if (currentArchive == null) return;

            if (!suppress)
            {
                this.previous.Add(directory);
            }

            if (directory is Arch archive)
            {
                this.list.Items.Clear();
                foreach (var item in archive.Children)
                {
                    if (item is FolderItem)
                        this.list.Items.Add(item);
                }

                foreach (var item in archive.Children)
                {
                    if (item is FileItem)
                        this.list.Items.Add(item);
                }

                current = archive;
            } 
            else if (directory is FolderItem folder)
            {
                this.list.Items.Clear();
                foreach (var item in folder.Children)
                {
                    if (item is FolderItem)
                        this.list.Items.Add(item);
                }

                foreach (var item in folder.Children)
                {
                    if (item is FileItem)
                        this.list.Items.Add(item);
                }

                current = folder;
            }
            else if (directory is FileItem file)
            {
                navigate(file.FullName, this.currentArchive);
            }

            this.breadcrumb.Navigate(directory);
        }

        private void navigate(string relativePath, Arch archive)
        {
            string[] fullpath = relativePath.Split('\\');
            FolderItem? parent = null;
            string folderName = fullpath.Last();
            string fullCascadeName = "";

            if (fullpath.Count() > 1)
            {
                string cascadeName = fullpath[0];
                fullCascadeName = fullCascadeName + cascadeName;
                parent = (FolderItem?) archive.Children.Find((x) => {
                    return (x.Name == cascadeName && x.IsFolder); 
                });

                if (parent == null) return;
                for (int cascade = 1; cascade < fullpath.Length - 1; cascade++)
                {
                    cascadeName = fullpath[cascade];
                    fullCascadeName = fullCascadeName + "\\" + cascadeName;
                    FolderItem? fi = (FolderItem?) parent.Children.Find((x) => { 
                        return x.Name == cascadeName; 
                    });
                    if (fi == null) return;
                    parent = fi;
                }

                FolderItem? found = (FolderItem?)parent.Children.Find((x) => {
                    return (x.Name == folderName && x.IsFolder);
                });

                if (found != null) navigate(found);
                else navigate(parent);

            } 
            else
            {
                FolderItem? found = (FolderItem?) archive.Children.Find((x) => {
                    return (x.Name == folderName && x.IsFolder);
                });

                if (found != null) navigate(found);
                else navigate(archive);
            }
        }

        private void resetPropertyStack()
        {
            this.sArchiveType.Visibility = Visibility.Collapsed;
            this.sPhysicalSize.Visibility = Visibility.Collapsed;
            this.sVol.Visibility = Visibility.Collapsed;
            this.sActualSize.Visibility = Visibility.Collapsed;
            this.sAttributes.Visibility = Visibility.Collapsed;
            this.sCompressionMethod.Visibility = Visibility.Collapsed;
            this.sCRC.Visibility = Visibility.Collapsed;
            this.sDateAccessed.Visibility = Visibility.Collapsed;
            this.sDateCreation.Visibility = Visibility.Collapsed;
            this.sDateModified.Visibility = Visibility.Collapsed;
            this.sEncrypted.Visibility = Visibility.Collapsed;
            this.sId.Visibility = Visibility.Collapsed;
        }

        private void updateProperties(FileSystemNode node)
        {
            resetPropertyStack();

            if (node is Arch arch)
            {
                this.sArchiveType.Visibility = Visibility.Visible;
                this.sPhysicalSize.Visibility = Visibility.Visible;
                this.sVol.Visibility = Visibility.Visible;

                this.tArchiveType.Text = arch.Format;
                this.tPhysicalSize.Text = Item.expressSize(arch.PackedSize);
                this.tVol.Text = arch.Volume.ToString();

                this.lblDesc.Content = arch.Name + " (" + this.tPhysicalSize.Text + ")";
            }

            if (node is Item i)
            {
                this.sActualSize.Visibility = Visibility.Visible;
                this.sAttributes.Visibility = Visibility.Visible;
                this.sCompressionMethod.Visibility = Visibility.Visible;
                this.sCRC.Visibility = Visibility.Visible;
                this.sDateAccessed.Visibility = Visibility.Visible;
                this.sDateCreation.Visibility = Visibility.Visible;
                this.sDateModified.Visibility = Visibility.Visible;
                this.sEncrypted.Visibility = Visibility.Visible;
                this.sId.Visibility = Visibility.Visible;

                this.tActualSize.Text = i.SizeHumanFriendly;
                this.tAttributes.Text = i.Attributes.ToString();
                this.tCompressionMethod.Text = i.Method;
                this.tCRC.Text = i.CRC.ToString();
                this.tDateAccessed.Text = i.DateAccessed.ToString();
                this.tDateCreation.Text = i.DateCreation.ToString();
                this.tDateModified.Text = i.DateModified.ToString();
                this.tEncrypted.Text = i.IsEncrypted.ToString();
                this.tId.Text = i.Index.ToString();

                this.lblDesc.Content = i.Name + " (" + this.tActualSize.Text + ")";
            }

            if (node is FileItem fi)
            {
                this.imgIcon.Source = IconExtension.GetIconFromExtension(fi.Name ?? "", SHIL.SHIL_JUMBO);
            } 
            else
            {
                this.imgIcon.Source = node.Icon;
            }
        }

        public RoutedUICommand OpenArchive { get; private set; }
        public RoutedUICommand ClearHistory { get; private set; }
        public RoutedUICommand Create { get; private set; }
        public RoutedUICommand Decompress { get; private set; }
        public RoutedUICommand DecompressToCurrent { get; private set; }
        public RoutedUICommand Reveal { get; private set; }
        public RoutedUICommand Quit { get; private set; }
        public RoutedUICommand Previous { get; private set; }
        public RoutedUICommand Next { get; private set; }
        public RoutedUICommand Root { get; private set; }
        public RoutedUICommand LevelUp { get; private set; }
        public RoutedUICommand Append { get; private set; }
        public RoutedUICommand Delete { get; private set; }
        public RoutedUICommand Rename { get; private set; }
        public RoutedUICommand Copy { get; private set; }
        public RoutedUICommand Paste { get; private set; }
        public RoutedUICommand Cut { get; private set; }
        public RoutedUICommand SelectAll { get; private set; }
        public RoutedUICommand Deselect { get; private set; }
        public RoutedUICommand Reverse { get; private set; }
        public RoutedUICommand SelectExtension { get; private set; }
        public RoutedUICommand About7Z { get; private set; }
        public RoutedUICommand About { get; private set; }

        private RoutedUICommand registerCommand(string description, string name, KeyGesture? gesture,
            Action<object, ExecutedRoutedEventArgs> action, Func<object, CanExecuteRoutedEventArgs, bool> enabled)
        {
            RoutedUICommand? command = null;
            if (gesture != null)
            {
                command = new RoutedUICommand(description, name, typeof(MainWindow),
                    new InputGestureCollection() { gesture });

                KeyBinding key = new KeyBinding(command, gesture);
                this.InputBindings.Add(key);
            } 
            else
            {
                command = new RoutedUICommand(description, name, typeof(MainWindow));
            }

            CommandBinding binding = new CommandBinding(command, 
                (s, e) => { action(s, e); }, 
                (s, e) => { e.CanExecute = enabled(s, e); });
            this.CommandBindings.Add(binding);
            return command;
        }

        private void titleBarDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void windowClose(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void windowStateChange(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
                return;
            }
            this.WindowState = WindowState.Minimized;
        }

        ListSortDirection direction = ListSortDirection.Ascending;
        string sortProperty = "Name";

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

        private void sort()
        {
            if (this.currentArchive == null) return;
            SortDescription desc = new SortDescription(sortProperty, direction);
            this.list.Items.SortDescriptions.Clear();
            this.list.Items.SortDescriptions.Add(desc);
        }

        public Arch? currentArchive { get; set; } = null;

        public bool IsArchiveOpened { get { return this.currentArchive != null; } }

        public FileSystemNode? current { get; set; } = null;
        List<FileSystemNode> previous = new List<FileSystemNode>();
        List<FileSystemNode> next = new List<FileSystemNode>();
    }

    // archiver file structures - these structures are core models for archiver
    // file representation and visualization.

    public class FileSystemNode
    {
        public string? Name { get; set; }
        public ImageSource? Icon { get; set; }

        public FileSystemNode? Parent { get; set; }

        public override string ToString()
        {
            return this.Name ?? base.ToString() ?? "<>";
        }
    }

    public class Arch : FileSystemNode, IEnumerable<Item>
    {
        public List<Item> Children { get; set; } = new List<Item>();
        public string FullName { get; set; } = "";
        public string Format { get; set; } = "";
        public ulong PackedSize { get; set; }
        public int Volume { get; set; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public IEnumerator<Item> GetEnumerator()
        {
            return this.Children.GetEnumerator();
        }

        public bool IsSPF
        {
            get
            {
                if (this.Children.Any())
                    if (this.Children.First() is FolderItem folder)
                        if ((folder.Name ?? "-").EndsWith(":"))
                            return true;
                return false;
            }
        }

        private FileItem? findPathRecursive(List<Item> items, string path)
        {
            foreach (var item in items)
            {
                if(item is FolderItem folder)
                {
                    var result = findPathRecursive(folder.Children, path);
                    if (result != null) return result;
                }
                else if(item is FileItem file)
                {
                    if (file.FullName == path)
                        return file;
                }
            }

            return null;
        }

        public FileItem? FindPathRecursive(string path)
        {
            return findPathRecursive(this.Children, path);
        }
    }

    public class Item : FileSystemNode
    {
        internal static string expressSize(ulong sizeB)
        {
            string expr = sizeB + " B  ";

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

        public string FullName { get; set; } = "";
        public bool IsFolder { get; set; }
        public virtual ulong Size { get; set; }
        public string SizeHumanFriendly
        {
            get
            {
                return expressSize(Size);
            }
        }
        public virtual DateTime? DateModified { get; set; }
        public virtual DateTime? DateCreation { get; set; }
        public virtual DateTime? DateAccessed { get; set; }
        public uint Attributes { get; set; }
        public virtual bool? IsEncrypted { get; set; }
        public uint CRC { get; set; }
        public string Method { get; set; } = "";
        public string? Comment { get; set; }
        public string? CommentAbstract
        {
            get
            {
                if (Comment == null) return null;
                return Comment.Split('\n').FirstOrDefault("");
            }
        }
        public int Index { get; set; }
    }

    public class FolderItem : Item, IEnumerable<Item>
    {
        public FolderItem(ArchiveFileInfo info)
        {
            this.Attributes = info.Attributes;
            this.Comment = info.Comment;
            this.CRC = info.Crc;
            // this.DateAccessed = info.LastAccessTime;
            // this.DateCreation = info.CreationTime;
            // this.DateModified = info.LastWriteTime;
            this.FullName = info.FileName;
            this.Name = info.FileName.Split('\\').Last();
            ImageSourceConverter isc = new ImageSourceConverter();
            this.Icon = isc.ConvertFrom("pack://siteoforigin:,,,/resources/folder.ico") as ImageSource;
            this.Index = info.Index;
            // this.IsEncrypted = info.Encrypted;
            this.IsFolder = info.IsDirectory;
            this.Method = info.Method;
            // this.Size = info.Size;
        }

        public FolderItem() { }

        public override DateTime? DateAccessed { 
            get 
            {
                return this.Children.Max(r => r.DateAccessed);
            } 
        }

        public override DateTime? DateCreation
        {
            get
            {
                return this.Children.Max(r => r.DateCreation);
            }
        }

        public override DateTime? DateModified
        {
            get
            {
                return this.Children.Max(r => r.DateModified);
            }
        }

        public override bool? IsEncrypted
        {
            get
            {
                return this.Children.Any((x) =>
                {
                    return x.IsEncrypted ?? false;
                });
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

        public List<Item> Children { get; set; } = new List<Item>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public IEnumerator<Item> GetEnumerator()
        {
            return this.Children.GetEnumerator();
        }
    }

    public class FileItem : Item
    {
        public FileItem(ArchiveFileInfo info)
        {
            this.Attributes = info.Attributes;
            this.Comment = info.Comment;
            this.CRC = info.Crc;
            this.DateAccessed = info.LastAccessTime;
            this.DateCreation = info.CreationTime;
            this.DateModified = info.LastWriteTime;
            this.FullName = info.FileName;
            this.Name = info.FileName.Split('\\').Last();
            this.Icon = IconExtension.GetIconFromExtension(this.Name);
            this.Index = info.Index;
            this.IsEncrypted = info.Encrypted;
            this.IsFolder = info.IsDirectory;
            this.Method = info.Method;
            this.Size = info.Size;
        }
    }

    internal static class IconExtension
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
            return GetIcon(GetIconIndex(fileName), flag);
        }

        public static ImageSource? GetIconFromExtension(string fileName, 
            Vanara.PInvoke.Shell32.SHIL flag = Vanara.PInvoke.Shell32.SHIL.SHIL_SMALL)
        {
            string ext = fileName.Split('.').Last();
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"temp\." + ext))
                File.Create(AppDomain.CurrentDomain.BaseDirectory + @"temp\." + ext);
            var icon = GetIconFromFile(AppDomain.CurrentDomain.BaseDirectory + @"temp\." + ext, flag);
            if (icon?.Width > 0)
                return icon.ToImageSource();
            else return null;
        }
    }

    public interface ILoadingStatusProvider
    {
        public void descriptionOn(string heading, string subheading);
        public void descriptionOff();
        public void setDescription(string heading, string subheading);

        public void loaderOn();
        public void loaderOff();

        public void progressOn(string description, int percentage);
        public void progressOff();
        public void reportProgress(string description, int percentage);

        public void cancelEnabled();
        public void cancelDisabled();
        public event EventHandler CancelRequest;

        public void proceedEnabled();
        public void proceedDisabled();
        public void changeProceedText(string content);
        public event EventHandler ProceedRequest;
        public void proceed();
    }

    public interface ITaskPage
    {
        public event EventHandler JobFinished;
    }

    public partial class MainWindow: ILoadingStatusProvider
    {
        public void descriptionOn(string heading, string subheading)
        {
            this.stackDoubleLineRemark.Visibility = Visibility.Visible;
            this.stackDblHeading.Text = heading;
            this.stackDblSubheading.Text = subheading;
        }

        public void descriptionOff()
        {
            this.stackDoubleLineRemark.Visibility = Visibility.Collapsed;
        }

        public void setDescription(string heading, string subheading)
        {
            this.stackDblHeading.Text = heading;
            this.stackDblSubheading.Text = subheading;
        }

        public void loaderOn()
        {
            this.loadingRing.Visibility = Visibility.Visible;
        }

        public void loaderOff()
        {
            this.loadingRing.Visibility = Visibility.Collapsed;
        }

        public void progressOn(string description, int percentage)
        {
            this.stackProgress.Visibility = Visibility.Visible;
            this.stackProgressBar.Value = percentage;
            this.stackProgressDescription.Text = description;
            this.stackProgressIndic.Text = percentage.ToString() + " %";
        }
        public void progressOff()
        {
            this.stackProgress.Visibility = Visibility.Collapsed;
        }

        public void reportProgress(string description, int percentage)
        {
            this.stackProgressBar.Value = percentage;
            this.stackProgressDescription.Text = description;
            this.stackProgressIndic.Text = percentage.ToString() + " %";
        }

        public void cancelEnabled()
        {
            this.btnCancel.IsEnabled = true;
        }

        public void cancelDisabled()
        {
            this.btnCancel.IsEnabled = false;
        }

        public event EventHandler CancelRequest;

        public void proceedEnabled()
        {
            this.btnProceed.IsEnabled = true;
        }

        public void proceedDisabled()
        {
            this.btnProceed.IsEnabled = false;
        }

        public void changeProceedText(string content)
        {
            this.btnProceed.Content = content;
        }

        public event EventHandler ProceedRequest;

        public void resetStatusBar()
        {
            this.loaderOff();
            this.descriptionOff();
            this.progressOff();
            this.cancelDisabled();
            this.proceedDisabled();

            foreach (var del in this.CancelRequest?.GetInvocationList() ?? new Delegate[] { })
                this.CancelRequest -= (EventHandler)del;

            foreach (var del in this.ProceedRequest?.GetInvocationList() ?? new Delegate[] { })
                this.ProceedRequest -= (EventHandler)del;

            this.changeProceedText("Proceed");
        }

        public void proceed()
        {
            if (this.btnProceed.IsEnabled)
                this.ProceedRequest?.Invoke(this.btnProceed, new EventArgs());
        }
    }
}
