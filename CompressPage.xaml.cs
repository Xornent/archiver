
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
                loader.proceedDisabled();
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

                if (entries.Count > 0)
                    loader.proceedEnabled();
                else loader.proceedDisabled();
            };

            #region Wildcard

            this.selectWd.Click += (s, e) => {
                // OpenFolderDialog folderSelect = new OpenFolderDialog();
                // folderSelect.Multiselect = false;
                // if (folderSelect.ShowDialog() == true)
                // {
                //     this.workingDirectory = new DirectoryInfo(folderSelect.FolderName);
                //     this.sourceParentalDirectory = this.workingDirectory;
                // 
                //     this.comboBoxDest.Items.Add(this.workingDirectory);
                //     this.comboWd.Items.Add(this.workingDirectory);
                //     this.comboBoxDest.SelectedIndex = this.comboBoxDest.Items.Count - 1;
                //     this.comboWd.SelectedIndex = this.comboWd.Items.Count - 1;
                //     dest = this.workingDirectory;
                // 
                //     updatedSearch = false;
                //     this.updatedExecution = false;
                // }
            };

            this.comboWd.SelectionChanged += (s, e) => {
                this.workingDirectory = this.comboWd.SelectedItem as DirectoryInfo;
                this.sourceParentalDirectory = this.workingDirectory;
                this.dest = this.workingDirectory;
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
                    // loader.changeProceedText("Select");
                    loader.proceedEnabled();
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

            #region Options

            this.radioWithPassword.Click += (s, e) =>
            {
                if (this.radioWithPassword.IsChecked ?? false)
                    this.panelPassword.Visibility = Visibility.Visible;
                else this.panelPassword.Visibility = Visibility.Collapsed;
            };

            this.radioNoPassword.Click += (s, e) =>
            {
                if (this.radioWithPassword.IsChecked ?? false)
                    this.panelPassword.Visibility = Visibility.Visible;
                else this.panelPassword.Visibility = Visibility.Collapsed;
            };

            // ID	                                    Тип параметра	Группа параметра
            // PARAMETER_ID_COMPRESSION_LEVEL           VT_UI4          PARAMETER_GROUP_COMPRESSION
            // PARAMETER_ID_COMPRESSION_METHOD          VT_BSTR
            // PARAMETER_ID_COMPRESSION_SUBMETHOD       VT_BSTR
            // PARAMETER_ID_COMPRESSION_DICTIONARYSIZE  VT_UI8
            // PARAMETER_ID_COMPRESSION_WORDSIZE        VT_UI4
            // PARAMETER_ID_COMPRESSION_THREADS         VT_UI4
            // PARAMETER_ID_COMPRESSION_SOLID           VT_BOOL
            // PARAMETER_ID_COMPRESSION_SOLIDBLOCKSIZE  VT_UI8
            // PARAMETER_ID_COMPRESSION_SORT            VT_BOOL
            // PARAMETER_ID_STORE_SYMLINKS              VT_BOOL         PARAMETER_GROUP_STORE
            // PARAMETER_ID_STORE_HARDLINKS             VT_BOOL
            // PARAMETER_ID_STORE_ALTSTREAMS            VT_BOOL
            // PARAMETER_ID_STORE_FILESECURITY          VT_BOOL
            // PARAMETER_ID_STORE_DATECREATED           VT_BOOL
            // PARAMETER_ID_STORE_DATEMODIFIED          VT_BOOL
            // PARAMETER_ID_STORE_DATEACCESSED          VT_BOOL
            // PARAMETER_ID_STORE_ATTRIBUTES            VT_BOOL
            // PARAMETER_ID_ENCRYPTION                  VT_BOOL         PARAMETER_GROUP_ENCRYPTION
            // PARAMETER_ID_ENCRYPTION_PASSWORD         VT_BSTR
            // PARAMETER_ID_ENCRYPTION_METHOD           VT_BSTR
            // PARAMETER_ID_ENCRYPTION_HEADER           VT_BOOL
            // PARAMETER_ID_ENCRYPTION_HEADERPASSWORD   VT_BSTR
            // PARAMETER_ID_SFX_CREATE                  VT_BOOL         PARAMETER_GROUP_SFX
            // PARAMETER_ID_SFX_MODULE                  VT_BSTR
            // PARAMETER_ID_CONTAINER_VOLUMES           VT_BOOL         PARAMETER_GROUP_CONTAINER
            // PARAMETER_ID_CONTAINER_VOLUMESIZE        VT_UI8
            // PARAMETER_ID_CONTAINER_COMMENTS          VT_BSTR

            this.comboArchiveType.SelectionChanged += (s, e) => {
                this.expZip.Visibility = Visibility.Collapsed;
                this.expBzip.Visibility = Visibility.Collapsed;
                this.expGzip.Visibility = Visibility.Collapsed;
                this.exp7z.Visibility = Visibility.Collapsed;
                this.expTar.Visibility = Visibility.Collapsed;
                this.expWim.Visibility = Visibility.Collapsed;

                // this.stSNS.Visibility = Visibility.Collapsed;
                // this.stSNI.Visibility = Visibility.Collapsed;

                switch ((comboArchiveType.SelectedItem as ComboBoxItem)?.Content?.ToString()?.ToLower() ?? "")
                {
                    case "zip":
                        this.expZip.Visibility = Visibility.Visible;
                        break;
                    case "bz2":
                        this.expBzip.Visibility = Visibility.Visible;
                        break;
                    case "gz":
                        this.expGzip.Visibility = Visibility.Visible;
                        break;
                    case "7z":
                        this.exp7z.Visibility = Visibility.Visible;
                        break;
                    case "tar":
                        this.expTar.Visibility = Visibility.Visible;
                        break;
                    case "wim":
                        this.expWim.Visibility = Visibility.Visible;
                        // this.stSNI.Visibility = Visibility.Visible;
                        // this.stSNS.Visibility = Visibility.Visible;
                        break;
                }
            };

            #endregion

            #region Compress

            loader.ProceedRequest += (s, e) =>
            {
                Archive.SevenZipCompressor compressor = new Archive.SevenZipCompressor();
                string archiveType = (comboArchiveType.SelectedItem as ComboBoxItem)?.Content?.ToString()?.ToLower() ?? "";
                compressor.CompressionMode = Archive.CompressionMode.Create;
                compressor.CompressionMethod = Library.CompressionMethod.Deflate;

                if (archiveType == "")
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        loader.descriptionOn("Compress error", "Invalid archive type");
                    });
                    return;
                }

                switch (archiveType)
                {
                    case "zip":
                        break;
                    case "bz2":
                        break;
                    case "gz":
                        break;
                    case "7z":
                        break;
                    case "tar":
                        break;
                    case "wim":
                        break;
                }

                string? password = this.radioWithPassword.IsChecked ?? false ?
                    this.txtPassword.Text : null;

                bool useSPF = this.chkSPF.IsChecked ?? false;

                Dictionary<string, string> fileDictionary = new Dictionary<string, string>();
                if (this.source == null || this.source.Count == 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        loader.descriptionOn("Compress error", "Must compress at least 1 file");
                    });
                    return;
                }

                string archiveName = this.txtArchiveName.Text.Trim();
                if (string.IsNullOrEmpty(archiveName))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        loader.descriptionOn("Compress error", "Must specify the archive name");
                    });
                    return;
                }

                foreach (var item in this.source)
                {
                    if (!useSPF) fileDictionary.Add(item.ArchivePath, item.FileSystemPath);
                    else fileDictionary.Add(item.FileSystemPath, item.FileSystemPath);
                }

                compressor.FilesFound += (s, e) => {
                    this.Dispatcher.Invoke(() =>
                    {
                        loader.descriptionOn("Scanning for files ...", $"{e.Value} / {fileDictionary.Count} files has been found.");
                    });
                };

                compressor.FileCompressionStarted += (s, e) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        loader.descriptionOn("Compressing ...", "Compressing files into archive");
                        loader.progressOn($"Compressing {e.FileName}", e.PercentDone);
                    });
                };

                compressor.Compressing += (s, e) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        // loader.progressOn($"Compressing ...", e.PercentDone);
                    });
                };

                compressor.CompressionFinished += (s, e) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        loader.descriptionOff();
                        loader.progressOff();
                        loader.loaderOff();
                        this.JobFinished?.Invoke(this, new EventArgs());
                    });
                };

                loader.loaderOn();
                string destination = this.dest?.FullName ?? "";
                if (destination == "") destination = archiveName + "." + archiveType;
                else if (destination.EndsWith("\\")) destination += archiveName + "." + archiveType;
                else destination += "\\" + archiveName + "." + archiveType;

                compressor.CustomParameters.Add("tm", "on");
                compressor.CustomParameters.Add("tc", "on");
                compressor.CustomParameters.Add("ta", "on");
                compressor.CustomParameters.Add("tp", "3");
                compressor.CustomParameters.Add("mt", "44");

                if (password == null)
                    compressor.BeginCompressFileDictionary(fileDictionary, destination);
                else compressor.BeginCompressFileDictionary(fileDictionary, destination, password);
            };

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

        public event EventHandler? JobFinished;

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
                        isc.ConvertFrom("pack://siteoforigin:,,,/resources/check.png") as ImageSource :
                        isc.ConvertFrom("pack://siteoforigin:,,,/resources/close.png") as ImageSource;
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

            internal WildcardInput(bool include = true, bool isLast = true, bool isDefault = false, int textboxSize = 215)
            {
                isc = new ImageSourceConverter();
                this.inputText = new TextBox();
                this.inputText.FontFamily = Application.Current.Resources["fixedWidthFont"] as FontFamily;
                this.inputText.Width = textboxSize;

                this.btnExcludeSwitch = new Button();
                StackPanel besContent = new StackPanel();
                besContent.Orientation = Orientation.Horizontal;
                this.btnExcludeSwitchImage = new Image();
                this.btnExcludeSwitchImage.Margin = new Thickness(2, 1, 2, 0);
                this.btnExcludeSwitchImage.Width = 16;
                this.btnExcludeSwitchImage.Height = 16;
                this.btnExcludeSwitchImage.Source = include ?
                    isc.ConvertFrom("pack://siteoforigin:,,,/resources/check.png") as ImageSource :
                    isc.ConvertFrom("pack://siteoforigin:,,,/resources/close.png") as ImageSource;
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
                btnAddImage.Source = isc.ConvertFrom("pack://siteoforigin:,,,/resources/add-circle.png") as ImageSource;
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
                btnRemoveImage.Source = isc.ConvertFrom("pack://siteoforigin:,,,/resources/remove-circle.png") as ImageSource;
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
