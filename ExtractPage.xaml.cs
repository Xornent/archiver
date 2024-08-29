using Archiver.Archive;
using Archiver.Exceptions;
using Archiver.UI;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using static Archiver.CompressPage;

namespace Archiver
{
    /// <summary>
    /// Interaction logic for ExtractPage.xaml
    /// </summary>
    public partial class ExtractPage : UserControl, ITaskPage
    {
        ILoadingStatusProvider loader;
        Arch archive;
        bool isSPF = false;

        private string currentWildcard = "**";
        private BackgroundWorker worker;
        private List<string> includeRules = new List<string>();
        private List<string> excludeRules = new List<string>();
        private List<string> matches = new List<string>();
        private string lastExecuteWildcast = "";
        private List<string> lastExecutionResult = new List<string>();
        private bool updatedExecution = false; 
        private List<WildcardInput> wildcardInputs = new List<WildcardInput>();

        private List<string> availableFiles = new List<string>();
        public FileEntryCollection source = new FileEntryCollection();

        public DirectoryInfo? extracted;
        private string remembered = "";
        private bool autoExtract = false;

        public ExtractPage(ILoadingStatusProvider provider, Arch arch, bool autoextract = false)
        {
            InitializeComponent();
            this.loader = provider;
            this.archive = arch;
            this.extracted = new FileInfo(arch.FullName).Directory;
            this.autoExtract = autoextract;

            this.chkSPF.IsEnabled = arch.IsSPF;
            this.chkSPF.IsChecked = arch.IsSPF;
            this.panelExclusion.Visibility = Visibility.Collapsed;
            this.panelPass.Visibility = Visibility.Collapsed;

            loader.cancelEnabled();

            loader.CancelRequest += (s, e) =>
            {
                this.JobFinished?.Invoke(this, new EventArgs());
            };

            this.radioExtractAll.Click += (s, e) =>
            {
                if (this.radioIncludePart.IsChecked ?? false)
                    this.panelExclusion.Visibility = Visibility.Visible;
                else this.panelExclusion.Visibility = Visibility.Collapsed;
            };

            this.radioIncludePart.Click += (s, e) =>
            {
                if (this.radioIncludePart.IsChecked ?? false)
                    this.panelExclusion.Visibility = Visibility.Visible;
                else this.panelExclusion.Visibility = Visibility.Collapsed;
            };

            this.radioPass.Click += (s, e) =>
            {
                if (this.radioPass.IsChecked ?? false)
                    this.panelPass.Visibility = Visibility.Visible;
                else this.panelPass.Visibility = Visibility.Collapsed;
            };

            this.radioNoPass.Click += (s, e) =>
            {
                if (this.radioPass.IsChecked ?? false)
                    this.panelPass.Visibility = Visibility.Visible;
                else this.panelPass.Visibility = Visibility.Collapsed;
            };

            worker = new BackgroundWorker();
            worker.RunWorkerCompleted += (s, e) => {
                if (this.currentWildcard.Trim() == this.lastExecuteWildcast)
                {
                    this.updatedExecution = true;
                    if (this.extracted != null &&
                        this.source.Count > 0)
                    {
                        loader.proceedEnabled();
                        loader.cancelEnabled();

                        if (this.autoExtract) { loader.proceed(); this.autoExtract = false; }
                    }
                }
                else
                {
                    this.includeRules.Clear();
                    this.excludeRules.Clear();
                    loader.proceedDisabled();
                    loader.cancelDisabled();

                    foreach (var line in this.currentWildcard.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            if (line.StartsWith("!"))
                                this.excludeRules.Add(line.Substring(1));
                            else this.includeRules.Add(line);
                        }
                    }

                    lastExecuteWildcast = this.currentWildcard.Trim();
                    worker.RunWorkerAsync();
                }
            };

            worker.DoWork += (s, e) => {

                Microsoft.Extensions.FileSystemGlobbing.Matcher matcher =
                    new Microsoft.Extensions.FileSystemGlobbing.Matcher();
                foreach (var line in includeRules)
                    matcher.AddInclude(line.Replace("\r", "").Trim());
                foreach (var line in excludeRules)
                    matcher.AddExclude(line.Replace("\r", "").Trim());

                if (this.availableFiles.Count == 0)
                    walkDirectory(this.archive);

                void walkDirectory(Archiver.FileSystemNode info, string basepath = "")
                {
                    // for now, i only care about files, not folders,
                    // even they may have some information (comments etc), i just ignore them.

                    if (info is Arch arch)
                    {
                        foreach (var item in arch.Children)
                            walkDirectory(item);
                    } 
                    else if (info is FolderItem folder)
                    {
                        foreach (var item in folder.Children)
                            walkDirectory(item, basepath + folder.Name + "/");
                    }
                    else if (info is FileItem file)
                    {
                        if (basepath.Contains(":")) this.isSPF = true;
                        this.availableFiles.Add(basepath.Replace(":", "") + file.Name);
                    }
                }

                PatternMatchingResult results = matcher.Match(availableFiles);

                this.Dispatcher.Invoke(() => {
                    string errorMsg = "All file access";
                    if (this.extracted == null)
                    {
                        errorMsg = "The extract directory is not set";
                        loader.proceedDisabled();
                    }

                    loader.descriptionOn($"{results.Files.Count()} matches, in {availableFiles.Count} files", errorMsg);
                    this.comboExclue.Items.Clear();
                    this.comboExclue.Items.Add($"{results.Files.Count()} matches, in {availableFiles.Count} files");
                    this.comboExclue.SelectedIndex = 0;
                });

                if (this.extracted == null) return;

                this.Dispatcher.Invoke(() => {
                    List<(string dir, string stem)> tuples = new List<(string, string)>();

                    string bpath = this.extracted.FullName;
                    // now we convert the path to the real extracted place.
                    this.isSPF = archive.IsSPF && (this.chkSPF.IsChecked ?? false); 
                    if (this.isSPF) bpath = "Computer";

                    foreach (var item in results.Files)
                    {
                        if (this.isSPF)
                            tuples.Add((item.Path, item.Path));
                        else if (this.chkRecurse.IsChecked ?? false)
                            tuples.Add(($"{item.Path.Replace(":", "")}", item.Path));
                        else tuples.Add((item.Path.Replace(":", "").Split("/").Last(), item.Path));
                    }

                    this.source = new FileEntryCollection(tuples);
                    this.tree.Model = new PreviewFileTreeModel(this.source, archive, bpath);
                    this.tree.SetIsExpanded(this.tree.Nodes[0], true);
                });
            };

            wildcardInputs.Add(new WildcardInput(true, true, true, 190));
            this.stackWildcardControls.Children.Add(wildcardInputs[0].frame);
            wildcardInputs[0].inputText.Text = "**";
            wildcardInputs[0].btnAdd.Click += addWildcard;
            wildcardInputs[0].btnAdd.Click += wildCardUpdate;
            wildcardInputs[0].inputText.TextChanged += wildCardUpdate;
            wildcardInputs[0].btnExcludeSwitch.Click += wildCardUpdate;

            this.btnSelectDest.Click += (s, e) => {
                // OpenFolderDialog folder = new OpenFolderDialog();
                // folder.Multiselect = false;
                // if(folder.ShowDialog() ?? false)
                // {
                //     this.extracted = new DirectoryInfo(folder.FolderName);
                //     this.comboBoxDest.Items.Add(this.extracted);
                //     // here this.extracted will be set again
                //     this.comboBoxDest.SelectedIndex = this.comboBoxDest.Items.Count - 1;
                // }
            };

            this.comboBoxDest.SelectionChanged += (s, e) =>
            {
                if (this.comboBoxDest.SelectedIndex == 0)
                    this.extracted = new FileInfo(arch.FullName).Directory;
                else this.extracted = this.comboBoxDest.SelectedItem as DirectoryInfo;
                wildCardUpdate(this, new EventArgs());
            };

            this.chkRecurse.Click += (s, e) =>
            {
                wildCardUpdate(this, new EventArgs());
            };

            wildCardUpdate(this, new EventArgs());

            loader.ProceedRequest += (s, e) =>
            {
                if (this.extracted == null) return;
                if (this.source.Count <= 0) return;

                if (archive.IsSPF && (this.chkSPF.IsChecked ?? false))
                {
                    var spfdlg = new ExtractSpf();
                    spfdlg.ShowDialog();
                    if (!(spfdlg.DialogResult ?? false))
                        return;
                }

                Archive.SevenZipExtractor extractor;

                if (this.radioPass.IsChecked ?? false)
                    extractor = new Archive.SevenZipExtractor(archive.FullName, this.txtPass.Password);
                else extractor = new Archive.SevenZipExtractor(archive.FullName);

                List<int> indices = new List<int>();

                foreach (var item in this.source)
                {
                    var fileNode = archive.FindPathRecursive(item.ArchivePath.Replace("/", "\\"));
                    if (fileNode == null) throw new Exception("Unexpected");
                    indices.Add(fileNode.Index);
                }

                extractor.PreserveDirectoryStructure = this.chkRecurse.IsChecked ?? true;

                loader.loaderOn();
                loader.descriptionOn("Extracting archive ...", $"0 out of {this.source.Count} files");
                loader.cancelDisabled();
                loader.proceedDisabled();

                int num = 0;
                extractor.FileExtractionStarted += (s, e) =>
                {
                    num++;
                    loader.progressOn("Extracting " + e.FileInfo.FileName, e.PercentDone);
                    loader.setDescription("Extracting archive ...", $"{num} out of {this.source.Count} files");
                };

                extractor.ExtractionFinished += (s, e) =>
                {
                    this.remembered = "";
                    if (extractor.HasExceptions)
                    {
                        foreach (var item in extractor.Exceptions)
                        {
                            if (item is UserCancelledException)
                                MessageBox.Show("User cancelled the operation");
                            else if (item is SevenZipException sze)
                                MessageBox.Show(sze.Message);
                            else throw item;
                        }

                        loader.loaderOff();
                        loader.descriptionOff();
                        loader.progressOff();
                        loader.cancelEnabled();
                        wildCardUpdate(this, new EventArgs());
                        return;
                    }

                    loader.loaderOff();
                    loader.descriptionOff();
                    loader.progressOff();
                    this.JobFinished?.Invoke(this, new EventArgs());
                };
                
                extractor.FileExists += (s, e) =>
                {
                    FileInfo exists = new FileInfo(e.FileName ?? throw new Exception());
                    ArchiveFileInfo ar = e.Archive;

                    switch (this.comboAO.SelectedIndex)
                    {
                        case 0: // Overwrite All existing files without prompt
                            break;
                        case 1: // Skip extracting of existing files
                            e.FileName = null;
                            break;
                        case 2: // Auto rename extracting file
                            string mut = e.FileName.GetMutatedFileName();
                            e.FileName = mut;
                            break;
                        case 3: // Auto rename existing file
                            string mut2 = e.FileName.GetMutatedFileName();
                            File.Move(e.FileName, mut2);
                            File.Delete(e.FileName);
                            break;
                        case 4: // Remind me

                            switch (remembered)
                            {
                                case "A": // Always
                                    return;
                                case "S": // Skip all
                                    e.FileName = null;
                                    return;
                                case "U": // Rename all
                                    string mut3 = e.FileName.GetMutatedFileName();
                                    e.FileName = mut3;
                                    return;
                                default:
                                    break;
                            }

                            FileConflict conflict = new FileConflict(
                                e.FileName, Item.expressSize(Convert.ToUInt64(exists.Length)), $"{exists.LastWriteTime.ToLongDateString()} {exists.LastWriteTime.ToLongTimeString()}",
                                ar.FileName, Item.expressSize(Convert.ToUInt64(ar.Size)), $"{ar.LastWriteTime.ToLongDateString()} {ar.LastWriteTime.ToLongTimeString()}"); ;
                            
                            bool success = false;
                            while (!success)
                            {
                                conflict = new FileConflict(
                                    e.FileName, Item.expressSize(Convert.ToUInt64(exists.Length)), $"{exists.LastWriteTime.ToLongDateString()} {exists.LastWriteTime.ToLongTimeString()}",
                                    ar.FileName, Item.expressSize(Convert.ToUInt64(ar.Size)), $"{ar.LastWriteTime.ToLongDateString()} {ar.LastWriteTime.ToLongTimeString()}");
                                conflict.ShowDialog();
                                success = conflict.DialogResult ?? false;
                            }

                            switch (conflict.Response)
                            {
                                case "A": // Always
                                    remembered = "A";
                                    break;
                                case "Y": // Yes
                                    break;
                                case "N": // No
                                    e.FileName = null;
                                    break;
                                case "S":
                                    e.FileName = null;
                                    remembered = "S";
                                    break;
                                case "U":
                                    string mut3 = e.FileName.GetMutatedFileName();
                                    e.FileName = mut3;
                                    remembered = "U";
                                    break;
                                case "Q":
                                    e.Cancel = true;
                                    extractor.AddException(new UserCancelledException("User cancelled the operation"));
                                    loader.cancelEnabled();
                                    loader.proceedEnabled();
                                    loader.loaderOff();
                                    loader.descriptionOff();
                                    loader.progressOff();
                                    break;
                                default:
                                    break;
                            }

                            break;
                        default:
                            break;
                    }
                };

                try
                {
                    extractor.BeginExtractFiles(this.extracted.FullName, indices.ToArray());
                }
                catch(Archiver.Exceptions.ExtractionFailedException extractFailed)
                {
                    MessageBox.Show("Your file may have password.");
                    extractor.AddException(extractFailed);
                }
                finally
                {
                    
                }
            };
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

                if (this.extracted != null)
                {
                    lastExecuteWildcast = this.currentWildcard.Trim();
                    worker.RunWorkerAsync();
                    loader.cancelDisabled();
                } else loader.proceedDisabled();
            }
        }

        private void addWildcard(object s, EventArgs e)
        {
            foreach (var item in wildcardInputs)
                item.IsLast = false;
            var append = new WildcardInput(true, true, false, 190);
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
    }

    public static class FileNameExtension
    {
        public static string GetParentalDirectory(this string path, char separator = '\\')
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException();
            if (path.EndsWith(separator))
                path = path.TrimEnd(separator);

            var segments = path.Split(separator);
            if (segments.Length <= 1) throw new ArgumentException();
            List<string> parts = segments.ToList();
            parts.RemoveAt(parts.Count - 1);

            return string.Join(separator, parts.ToArray());
        }

        public static string GetFileName(this string path, char separator = '\\')
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException();
            if (path.EndsWith(separator)) throw new ArgumentException();
            if (!path.Contains(separator))
                return path;

            var segments = path.Split(separator);
            if (segments.Length <= 1) throw new ArgumentException();
            return segments.Last();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="separator"></param>
        /// <param name="noext"></param>
        /// <returns>Returns `noext` if the file contains no extension</returns>
        /// <exception cref="ArgumentException"></exception>
        public static string GetExtension(this string path, char separator = '.', string noext = "")
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException();
            string fname = path.GetFileName();

            if (!fname.Contains(separator))
                return noext;

            if (fname.EndsWith(separator))
                return noext;
            
            var segments = fname.Split(separator);
            if (segments.Length <= 1) throw new ArgumentException();
            return segments.Last();
        }

        public static string GetFileNameWithoutExtension(this string path, char separator = '.')
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException();
            string fname = path.GetFileName();
            string ext = fname.GetExtension(separator);

            fname = fname.Substring(0, fname.Length - ext.Length);
            if (fname.EndsWith(separator)) fname = fname.TrimEnd(separator);
            return fname;
        }

        public static string GetMutatedFileName(this string fullpath)
        {
            int dup = 1;
            string mutated = fullpath;
            while(File.Exists(mutated))
            {
                mutated = $"{fullpath.GetParentalDirectory()}\\{fullpath.GetFileNameWithoutExtension()} ({dup}).{fullpath.GetExtension()}";
                dup++;
            }
            return mutated;
        }
    }
}
