
using Archiver.UI;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Vanara.PInvoke.User32;

namespace Archiver
{
    public partial class CompressPage : UserControl, ITaskPage
    {
        private DirectoryInfo? workingDirectory = null;
        private FileEntryCollection? source = null;
        private DirectoryInfo? sourceParentalDirectory = null;
        private DirectoryInfo? dest = null;

        private bool isWildcardMode = false;
        BackgroundWorker worker;
        private string currentWildcard = "";
        private List<string> includeRules = new List<string>();
        private List<string> excludeRules = new List<string>();
        private List<string> matches = new List<string>();
        private bool updatedSearch = false;
        private int errors = 0;
        private string lastExecuteWildcast = "";
        private List<string> lastExecutionResult = new List<string>();
        private bool updatedExecution = false;
        private bool pendingStart = false;
        private List<string> availableFiles = new List<string>();

        ILoadingStatusProvider loader;

        private List<WildcardInput> wildcardInputs = new List<WildcardInput>();

        private string methodArgs = "";
        private bool isSPF = false;

        public CompressPage(ILoadingStatusProvider provider)
        {
            InitializeComponent();
            this.loader = provider;

            loader.cancelEnabled();
            loader.CancelRequest += (s, e) =>
            {
                this.JobFinished?.Invoke(this, new EventArgs());
            };

            this.btnPickerMode.Click += (s, e) =>
            {
                // if switching to this mode, all selections prior will preserve.
                isWildcardMode = false;
                this.comboBoxSource.Items.Clear();
                this.comboBoxSource.Items.Add(this.source);
                this.comboBoxSource.SelectedIndex = 0;

                this.btnPickerMode.IsEnabled = false;
                this.btnWildcardMode.IsEnabled = true;
                this.uiPicker.Visibility = Visibility.Visible;
                this.uiWildcard.Visibility = Visibility.Collapsed;
            };

            this.btnWildcardMode.Click += (s, e) =>
            {
                // if switching to this mode, all selections prior will be cleared.
                this.source = new FileEntryCollection();
                this.tree.Model = new FileTreeModel(source);

                this.btnPickerMode.IsEnabled = true;
                this.btnWildcardMode.IsEnabled = false;
                this.uiPicker.Visibility = Visibility.Collapsed;
                this.uiWildcard.Visibility = Visibility.Visible;
                isWildcardMode = true;
            };

            this.selectSource.Click += (s, e) => {

                var dialog = new FileSelector();

                FileEntryCollection entries = new FileEntryCollection();
                if (this.chkAppend.IsChecked ?? false)
                    if (this.source != null)
                        foreach (var item in this.source)
                            entries.Add(item);

                if (dialog.ShowDialog() == true)
                {
                    foreach (var path in dialog.SelectedEntries)
                        entries.Add(path);

                    this.comboBoxSource.Items.Clear();
                    this.comboBoxSource.Items.Add(entries);
                    this.comboBoxSource.SelectedIndex = 0;

                    source = entries;

                    sourceParentalDirectory = dialog.ParentalDirectory;

                    this.comboBoxDest.Items.Add(sourceParentalDirectory);
                    dest = sourceParentalDirectory;
                    this.comboBoxDest.IsEnabled = true;
                    this.comboBoxDest.SelectedIndex = this.comboBoxDest.Items.Count - 1;

                    this.tree.Model = new FileTreeModel(entries);
                }
            };

            #region Wildcard

            this.selectWd.Click += (s, e) => {
                OpenFolderDialog folderSelect = new OpenFolderDialog();
                folderSelect.Multiselect = false;
                if (folderSelect.ShowDialog() == true)
                {
                    this.workingDirectory = new DirectoryInfo(folderSelect.FolderName);
                    this.sourceParentalDirectory = this.workingDirectory;

                    this.comboBoxDest.Items.Add(this.workingDirectory);
                    this.comboWd.Items.Add(this.workingDirectory);
                    this.comboBoxDest.SelectedIndex = this.comboBoxDest.Items.Count - 1;
                    this.comboWd.SelectedIndex = this.comboWd.Items.Count - 1;

                    updatedSearch = false;
                    this.updatedExecution = false;
                }
            };

            this.comboWd.SelectionChanged += (s, e) => {
                this.workingDirectory = this.comboWd.SelectedItem as DirectoryInfo;
                this.sourceParentalDirectory = this.workingDirectory;
                this.updatedSearch = false;
                this.updatedExecution = false;
            };

            worker = new BackgroundWorker();

            worker.RunWorkerCompleted += (s, e) => {

                loader.cancelEnabled();
                if (!isWildcardMode && !pendingStart) return;
                if (pendingStart) pendingStart = false;

                if (this.currentWildcard.Trim() == this.lastExecuteWildcast)
                {
                    this.updatedExecution = true;
                    loader.changeProceedText("Select");
                }
                else
                {
                    this.includeRules.Clear();
                    this.excludeRules.Clear();

                    foreach (var line in this.currentWildcard.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            if (line.StartsWith("!"))
                                this.excludeRules.Add(line.Substring(1));
                            else this.includeRules.Add(line);
                        }
                    }

                    if (this.workingDirectory != null)
                    {
                        lastExecuteWildcast = this.currentWildcard.Trim();
                        worker.RunWorkerAsync();
                        loader.cancelDisabled();
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
                    foreach (var dirs in info.GetDirectories())
                    {
                        try
                        {
                            if (!dirs.Attributes.HasFlag(FileAttributes.System))
                                error += walkDirectory(dirs);
                        } catch { error += 1; }
                    }

                    foreach (var files in info.GetFiles())
                    {
                        try
                        {
                            if (!files.Attributes.HasFlag(FileAttributes.System))
                                availableFiles.Add(files.FullName);
                        } catch { error += 1; }
                    }

                    return error;
                }

                if (this.workingDirectory == null) return;

                if (!updatedSearch)
                {
                    this.Dispatcher.Invoke(() => {
                        loader.loaderOn();
                        loader.descriptionOn("Generating indices ...", "Directory: " + this.workingDirectory?.Name ?? "None");
                    });

                    availableFiles.Clear();
                    errors = 0;
                    errors = walkDirectory(this.workingDirectory);
                    updatedSearch = true;
                }

                PatternMatchingResult results = matcher.Match(this.workingDirectory.FullName, availableFiles);

                this.Dispatcher.Invoke(() => {
                    loader.loaderOff();
                    string errorMsg = "All file access";
                    if (errors > 0)
                        errorMsg = $"{errors} unauthorized touches";

                    loader.descriptionOn($"{results.Files.Count()} matches, in {availableFiles.Count} files", errorMsg);
                });

                this.Dispatcher.Invoke(() => {
                    List<(string dir, string stem)> tuples = new List<(string, string)>();

                    foreach (var item in results.Files)
                    {
                        tuples.Add((Path.Combine(this.workingDirectory.FullName, item.Path), item.Stem ?? ""));
                    }

                    this.source = new FileEntryCollection(tuples);
                    this.tree.Model = new FileTreeModel(this.source);
                });

            };

            wildcardInputs.Add(new WildcardInput(true, true, true));
            this.stackWildcardControls.Children.Add(wildcardInputs[0].frame);
            wildcardInputs[0].btnAdd.Click += addWildcard;
            wildcardInputs[0].btnAdd.Click += wildCardUpdate;
            wildcardInputs[0].inputText.TextChanged += wildCardUpdate;
            wildcardInputs[0].btnExcludeSwitch.Click += wildCardUpdate;

            #endregion
        }

        private void wildCardUpdate(object s, EventArgs e)
        {
            this.currentWildcard = "";
            foreach (var item in this.wildcardInputs)
            {
                if (item.IsIncluded)
                    this.currentWildcard += item.Text + "\n";
                else this.currentWildcard += "!" + item.Text + "\n";
            }

            this.currentWildcard = this.currentWildcard.Trim();
            updatedExecution = false;

            if (!worker.IsBusy)
            {
                this.includeRules.Clear();
                this.excludeRules.Clear();

                foreach (var line in this.currentWildcard.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (line.StartsWith("!"))
                            this.excludeRules.Add(line.Substring(1));
                        else this.includeRules.Add(line);
                    }
                }

                if (this.workingDirectory != null)
                {
                    lastExecuteWildcast = this.currentWildcard.Trim();
                    worker.RunWorkerAsync();
                    loader.cancelDisabled();
                }
            }
        }

        private void addWildcard(object s, EventArgs e)
        {
            foreach (var item in wildcardInputs)
                item.IsLast = false;
            var append = new WildcardInput(true, true, false);
            wildcardInputs.Add(append);
            this.stackWildcardControls.Children.Add(append.frame);

            append.btnAdd.Click += addWildcard;
            append.btnRemove.Click += removeWildcard;

            append.btnAdd.Click += wildCardUpdate;
            append.btnRemove.Click += wildCardUpdate;
            append.inputText.TextChanged += wildCardUpdate;
            append.btnExcludeSwitch.Click += wildCardUpdate;
        }

        private void removeWildcard(object s, EventArgs e)
        {
            if (s is Button btnRemove)
            {
                if (btnRemove.Tag is WildcardInput inp)
                {
                    var id = this.wildcardInputs.IndexOf(inp);
                    if (id <= 0) return; // do not remove the first one.
                    if (id == this.wildcardInputs.Count - 1)
                        this.wildcardInputs[this.wildcardInputs.Count - 2].IsLast = true;

                    this.wildcardInputs.RemoveAt(id);

                    int cid = 0;
                    for (int i = 0; i < this.stackWildcardControls.Children.Count; i++)
                        if (this.stackWildcardControls.Children[i] == inp.frame)
                            cid = i;

                    this.stackWildcardControls.Children.RemoveAt(cid);
                }
            }
        }

        public event EventHandler JobFinished;

        internal class WildcardInput
        {
            internal StackPanel frame;
            internal TextBox inputText;

            internal Button btnExcludeSwitch;
            internal Image btnExcludeSwitchImage;
            internal TextBlock btnExcludeSwitchText;

            internal Button btnAdd;
            internal Button btnRemove;

            private bool isIncluded = true;
            private bool isLast = true;
            private bool isDefault = false;
            ImageSourceConverter isc;

            internal bool IsIncluded
            {
                get { return isIncluded; }
                set
                {
                    this.isIncluded = value;
                    this.btnExcludeSwitchImage.Source = value ?
                        isc.ConvertFrom("resources/check.png") as ImageSource :
                        isc.ConvertFrom("resources/close.png") as ImageSource;
                    this.btnExcludeSwitchText.Text = value ? "Include" : "Exclude";
                }
            }

            internal bool IsLast
            {
                get { return isLast; }
                set
                {
                    this.isLast = value;
                    this.btnAdd.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                    this.btnRemove.Visibility = isDefault ? Visibility.Collapsed : Visibility.Visible;
                }
            }

            internal string Text
            {
                get { return this.inputText.Text.Trim(); }
            }

            internal WildcardInput(bool include = true, bool isLast = true, bool isDefault = false)
            {
                isc = new ImageSourceConverter();
                this.inputText = new TextBox();
                this.inputText.FontFamily = Application.Current.Resources["fixedWidthFont"] as FontFamily;
                this.inputText.Width = 215;

                this.btnExcludeSwitch = new Button();
                StackPanel besContent = new StackPanel();
                besContent.Orientation = Orientation.Horizontal;
                this.btnExcludeSwitchImage = new Image();
                this.btnExcludeSwitchImage.Margin = new Thickness(2, 1, 2, 0);
                this.btnExcludeSwitchImage.Width = 16;
                this.btnExcludeSwitchImage.Height = 16;
                this.btnExcludeSwitchImage.Source = include ?
                    isc.ConvertFrom("resources/check.png") as ImageSource :
                    isc.ConvertFrom("resources/close.png") as ImageSource;
                this.btnExcludeSwitchText = new TextBlock();
                this.btnExcludeSwitchText.Margin = new Thickness(2, 0, 2, 0);
                this.btnExcludeSwitchText.Text = include ?
                    "Include" : "Exclude";
                besContent.Children.Add(this.btnExcludeSwitchImage);
                besContent.Children.Add(this.btnExcludeSwitchText);
                this.btnExcludeSwitch.Content = besContent;
                this.btnExcludeSwitch.Width = 85;

                var btnAddImage = new Image();
                btnAddImage.Margin = new Thickness(2, 0, 2, 0);
                btnAddImage.Width = 16;
                btnAddImage.Height = 16;
                btnAddImage.IsHitTestVisible = false;
                btnAddImage.Source = isc.ConvertFrom("resources/add-circle.png") as ImageSource;
                this.btnAdd = new Button();
                btnAdd.Width = 35;
                btnAdd.Margin = new Thickness(3);
                btnAdd.Style = Application.Current.Resources["secondaryButton"] as Style;
                btnAdd.Content = btnAddImage;

                var btnRemoveImage = new Image();
                btnRemoveImage.Margin = new Thickness(2, 0, 2, 0);
                btnRemoveImage.Width = 16;
                btnRemoveImage.Height = 16;
                btnRemoveImage.IsHitTestVisible = false;
                btnRemoveImage.Source = isc.ConvertFrom("resources/remove-circle.png") as ImageSource;
                this.btnRemove = new Button();
                btnRemove.Width = 35;
                btnRemove.Margin = new Thickness(3);
                btnRemove.Style = Application.Current.Resources["secondaryButton"] as Style;
                btnRemove.Content = btnRemoveImage;

                this.frame = new StackPanel();
                this.frame.Orientation = Orientation.Horizontal;
                this.frame.Children.Add(this.inputText);
                this.frame.Children.Add(this.btnExcludeSwitch);
                this.frame.Children.Add(this.btnAdd);
                this.frame.Children.Add(this.btnRemove);

                if (!isLast)
                {
                    this.btnAdd.Visibility = Visibility.Collapsed;
                    this.btnRemove.Visibility = Visibility.Visible;
                }

                if (isDefault)
                {
                    this.btnRemove.Visibility = Visibility.Collapsed;
                }

                this.isDefault = isDefault;
                this.isLast = isLast;
                this.isIncluded = include;

                this.btnExcludeSwitch.Click += (s, e) =>
                {
                    this.IsIncluded = !this.IsIncluded;
                };

                this.btnRemove.Tag = this;
            }
        }
    }
}
