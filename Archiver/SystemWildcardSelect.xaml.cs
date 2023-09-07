using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.ComponentModel;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Archiver.UI;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Text.RegularExpressions;

namespace Archiver
{
    /// <summary>
    /// Interaction logic for Extract.xaml
    /// </summary>
    public partial class SystemWildcardSelect : Window
    {
        public SystemWildcardSelect()
        {
            InitializeComponent();
            this.comboBoxDest.ItemTemplate = (DataTemplate)this.Resources["dirTemplate"];
            this.loadingRing.Scale = 0.5f;
            this.selectDest.Click += (s, e) => {
                FolderSelector folderSelect = new FolderSelector();
                if (folderSelect.ShowDialog() == true) {
                    this.WorkingDirectory = folderSelect.SelectedDirectory;
                    this.ParentalDirectory = this.WorkingDirectory;
                    this.comboBoxDest.Items.Add(folderSelect.SelectedDirectory);
                    this.comboBoxDest.SelectedIndex = this.comboBoxDest.Items.Count - 1;
                    updatedSearch = false;
                    this.updatedExecution = false;
                }
            };

            this.comboBoxDest.SelectionChanged += (s, e) => {
                this.WorkingDirectory = this.comboBoxDest.SelectedItem as DirectoryInfo;
                this.ParentalDirectory = this.WorkingDirectory;
                this.updatedSearch = false;
                this.updatedExecution = false;
            };

            BackgroundWorker worker = new BackgroundWorker();
            worker.RunWorkerCompleted += (s, e) => {
                if(this.txtWildcard.Text.Trim() == this.lastExecuteWildcast) {
                    this.updatedExecution = true;
                    this.IsEnabled = true;
                    this.btnOK.Content = "Select";
                } else {
                    this.includeRules.Clear();
                    this.excludeRules.Clear();

                    foreach (var line in this.txtWildcard.Text.Split('\n')) {
                        if (!string.IsNullOrWhiteSpace(line)) {
                            if (line.StartsWith("!"))
                                this.excludeRules.Add(line.Substring(1));
                            else this.includeRules.Add(line);
                        }
                    }

                    if (this.WorkingDirectory != null) {
                        lastExecuteWildcast = this.txtWildcard.Text.Trim();
                        worker.RunWorkerAsync();
                    }
                }
            };

            worker.DoWork += (s, e) => {

                Microsoft.Extensions.FileSystemGlobbing.Matcher matcher =
                    new Microsoft.Extensions.FileSystemGlobbing.Matcher();
                foreach (var line in includeRules)
                    matcher.AddInclude(line.Replace("\r", "").Trim());
                foreach (var line in excludeRules)
                    matcher.AddExclude(line.Replace("\r", "").Trim());

                List<string> placeholders = new List<string>();

                int walkDirectory(DirectoryInfo info)
                {
                    int error = 0;
                    foreach (var dirs in info.GetDirectories()) {
                        try {
                            if (!dirs.Attributes.HasFlag(FileAttributes.System))
                                error += walkDirectory(dirs);
                        } catch { error += 1; }
                    }

                    foreach (var files in info.GetFiles()) {
                        try {
                            if (!files.Attributes.HasFlag(FileAttributes.System))
                                availableFiles.Add(files.FullName);
                        } catch { error += 1; }
                    }

                    return error;
                }

                this.Dispatcher.Invoke(() => {
                    this.fileTree.IsEnabled = false;
                });

                if (!updatedSearch) {
                    this.Dispatcher.Invoke(() => {
                        this.loadingRing.Visibility = Visibility.Visible;
                        this.lblMessage.Content = "Generating Indices ...";
                    });
                    availableFiles.Clear();
                    errors = 0;
                    errors = walkDirectory(this.WorkingDirectory);
                    updatedSearch = true;
                }

                PatternMatchingResult results = matcher.Match(this.WorkingDirectory.FullName, availableFiles);

                this.Dispatcher.Invoke(() => {
                    this.loadingRing.Visibility = Visibility.Collapsed;
                    this.lblMessage.Content = $"{results.Files.Count()} matches, in {availableFiles.Count} files";
                    if (errors > 0) {
                        this.lblError.Visibility = Visibility.Visible;
                        this.lblError.Content = $"{errors} unauthorized touches";
                    } else this.lblError.Visibility = Visibility.Collapsed;
                });

                this.Dispatcher.Invoke(() => {
                    this.fileTree.Items.Clear();

                    List<UI.IconTreeNode> root = new List<IconTreeNode>();
                    this.lastExecutionResult.Clear();
                    foreach (var item in results.Files) {
                        
                        List<string> directories = new List<string>();

                        if (!(checkShowAsFileSystem.IsChecked ?? false)) {
                            directories = item.Stem.Split('/').ToList();
                            this.lastExecutionResult.Add(item.Stem);
                        } else { 
                            directories = item.Path.Split('/').ToList();
                            this.lastExecutionResult.Add(item.Path);
                        }

                        ImageSourceConverter isc = new ImageSourceConverter();

                        string cascade = directories[0];
                        if (directories.Count == 1) {
                            string ext = cascade.Split('.').Last();
                            if (ext == cascade) ext = "";

                            if (!File.Exists(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext))
                                File.Create(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext);
                            var icon = MainWindow.GetIconFromFile(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext, Vanara.PInvoke.Shell32.SHIL.SHIL_SMALL);
                            var node = new UI.IconTreeNode();
                            node.Caption = cascade;

                            if (icon?.Width > 0)
                                node.Icon = icon.ToImageSource();
                            root.Add(node);

                        } else {

                            bool flag = false;
                            foreach (IconTreeNode nodes in root) {
                                if (nodes.Caption == cascade) {
                                    flag = true;
                                    break;
                                }
                            }

                            if (!flag) {
                                var parent = new IconTreeNode();
                                parent.Icon = (ImageSource)isc.ConvertFrom("pack://siteoforigin:,,,/resources/folder.ico");
                                parent.Caption = cascade;
                                parent.Items.Add(new IconTreeNode() { Caption = "#" });
                                parent.Prefix = cascade + '/';
                                root.Add(parent);
                            }
                        }
                    }

                    foreach (var item in root) {
                        this.fileTree.Items.Add(item);
                    }

                    this.Dispatcher.Invoke(() => {
                        this.fileTree.IsEnabled = true;
                    });
                });

            };

            Action<object, object> wildCardUpdate = (s, e) => {
                updatedExecution = false;

                if (!worker.IsBusy) {
                    this.includeRules.Clear();
                    this.excludeRules.Clear();

                    foreach (var line in this.txtWildcard.Text.Split('\n')) {
                        if (!string.IsNullOrWhiteSpace(line)) {
                            if (line.StartsWith("!"))
                                this.excludeRules.Add(line.Substring(1));
                            else this.includeRules.Add(line);
                        }
                    }

                    if (this.WorkingDirectory != null) {
                        lastExecuteWildcast = this.txtWildcard.Text.Trim();
                        worker.RunWorkerAsync();
                    }
                }
            };

            this.txtWildcard.TextChanged += (s, e) => {
                wildCardUpdate.Invoke(s, e);
            };

            this.checkShowAsFileSystem.Checked += (s, e) => {
                wildCardUpdate.Invoke(s, e);
            };

            this.checkShowAsFileSystem.Unchecked += (s, e) => {
                wildCardUpdate.Invoke(s, e);
            };

            this.btnOK.Click += (s, e) => {
                if (updatedSearch && updatedExecution) {
                    List<(string dir, string stem)> tuples = new List<(string, string)>();

                    Microsoft.Extensions.FileSystemGlobbing.Matcher matcher =
                    new Microsoft.Extensions.FileSystemGlobbing.Matcher();
                    foreach (var line in includeRules)
                        matcher.AddInclude(line.Replace("\r", "").Trim());
                    foreach (var line in excludeRules)
                        matcher.AddExclude(line.Replace("\r", "").Trim());
                    PatternMatchingResult results = matcher.Match(this.WorkingDirectory.FullName, availableFiles);

                    foreach (var item in results.Files) {
                        tuples.Add((
                            Path.Combine(this.WorkingDirectory.FullName, item.Path), item.Stem));
                    }

                    this.SelectedEntries = new FileEntryCollection(tuples);
                    this.DialogResult = true;
                    this.Close();
                } else {
                    wildCardUpdate.Invoke(null, null);
                    this.btnOK.Content = "Please Wait ...";
                    this.IsEnabled = false;
                }
            };

            this.btnCancel.Click += (s, e) => {
                this.DialogResult = false;
                this.Close();
            };
        }

        private List<string> includeRules = new List<string>();
        private List<string> excludeRules = new List<string>();
        private List<string> matches = new List<string>();
        private bool updatedSearch = false;
        private int errors = 0;
        private string lastExecuteWildcast = "";
        private List<string> lastExecutionResult = new List<string>();
        private bool updatedExecution = false;

        private List<string> availableFiles = new List<string>();
        public FileEntryCollection SelectedEntries = new FileEntryCollection();
        public DirectoryInfo ParentalDirectory = null;

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

        public DirectoryInfo WorkingDirectory { get; private set; } = null;

        private void treeViewItemExpanded(object sender, RoutedEventArgs e)
        {
            ImageSourceConverter isc = new ImageSourceConverter();
            var tvi = e.OriginalSource as TreeViewItem;
            var src = tvi.Header as UI.IconTreeNode;
            if (src == null) return;
            if (src.Prefix == "") return;
            
            if(this.updatedExecution) {
                if(src.Items.Count == 1) {
                    if ((src.Items[0] as UI.IconTreeNode).Caption != "#") return;
                    src.Items.Clear();
                    foreach (var paths in this.lastExecutionResult) {
                        if(paths.StartsWith(src.Prefix)) {
                            string residualPath = paths.Substring(src.Prefix.Length);
                            string[] pieces = residualPath.Split('/');
                            if (pieces.Count() == 0) continue;

                            string cascade = pieces[0];
                            if (pieces.Length == 1) {
                                string ext = cascade.Split('.').Last();
                                if (ext == cascade) ext = "";

                                if (!File.Exists(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext))
                                    File.Create(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext);
                                var icon = MainWindow.GetIconFromFile(System.Windows.Forms.Application.StartupPath + @"\temp\." + ext, Vanara.PInvoke.Shell32.SHIL.SHIL_SMALL);
                                var node = new UI.IconTreeNode();
                                node.Caption = cascade;

                                if (icon?.Width > 0)
                                    node.Icon = icon.ToImageSource();
                                src.Items.Add(node);

                            } else {

                                bool flag = false;
                                foreach (IconTreeNode nodes in src.Items) {
                                    if (nodes.Caption == cascade) {
                                        flag = true;
                                        break;
                                    }
                                }

                                if (!flag) {
                                    var parent = new IconTreeNode();
                                    parent.Icon = (ImageSource)isc.ConvertFrom("pack://siteoforigin:,,,/resources/folder.ico");
                                    parent.Caption = cascade;
                                    parent.Items.Add(new IconTreeNode() { Caption = "#" });
                                    parent.Prefix = src.Prefix + cascade + '/';
                                    src.Items.Add(parent);
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
