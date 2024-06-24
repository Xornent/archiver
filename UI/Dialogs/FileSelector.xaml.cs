using Microsoft.WindowsAPICodePack.Controls;
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

namespace Archiver
{
    public partial class FileSelector : Window
    {
        PaneVisibilityState recordDetailsPanel;
        PaneVisibilityState recordNavigationPanel;
        public FileSelector()
        {
            InitializeComponent();
            this.explorer.NavigationTarget = ShellObject.FromParsingName("C:");

            this.ctxMenu.PlacementTarget = this.btnOptions;

            this.recordDetailsPanel = this.explorer.PreviewPane;
            this.recordNavigationPanel = this.explorer.NavigationPane;
            if (this.recordDetailsPanel == PaneVisibilityState.Show)
                this.details.IsChecked = true;
            if (this.recordNavigationPanel == PaneVisibilityState.Show)
                this.navigations.IsChecked = true;

            this.btnCancel.Click += (s, e) => {
                this.DialogResult = false;
                this.Close();
            };

            this.btnOK.Click += (s, e) => {

                this.SelectedDirectories.Clear();
                this.SelectedFiles.Clear();

                foreach (var item in this.explorer.SelectedItems)
                {
                    if (item.IsFileSystemObject)
                    {
                        if (item is ShellFile file)
                        {
                            this.SelectedFiles.Add(new FileInfo(file.ParsingName));
                        }
                        else if (item is ShellFolder folder)
                        {

                            if (folder.ParsingName.EndsWith(":\\"))
                            {
                                MessageBox.Show("You cannot select a drive.");
                                return;
                            }

                            // zip files are considered shell folders!
                            if (File.Exists(folder.ParsingName))
                                this.SelectedFiles.Add(new FileInfo(folder.ParsingName));
                            else this.SelectedDirectories.Add(new DirectoryInfo(folder.ParsingName));
                        }
                    }
                }

                this.ParentalDirectory = new DirectoryInfo((explorer.NavigationLog.Last() as ShellFolder).ParsingName);
                this.SelectedEntries = new FileEntryCollection(SelectedFiles, SelectedDirectories);

                if (this.SelectedDirectories.Count > 0 || this.SelectedFiles.Count > 0)
                {
                    this.DialogResult = true;
                    this.Close();
                } 
                else
                {
                    MessageBox.Show("You have not selected anything.");
                }
            };

            this.Closed += (s, e) =>
            {
                this.explorer.PreviewPane = this.recordDetailsPanel;
                this.explorer.NavigationPane = this.recordNavigationPanel;
            };

            this.btnOptions.Click += (s, e) =>
            {
                this.btnOptions.ContextMenu.IsOpen = true;
            };

            this.navigations.Click += (s, e) =>
            {
                if (this.navigations.IsChecked)
                    this.explorer.NavigationPane = PaneVisibilityState.Show;
                else this.explorer.NavigationPane = PaneVisibilityState.Hide;
            };

            this.details.Click += (s, e) =>
            {
                if (this.details.IsChecked)
                    this.explorer.DetailsPane = PaneVisibilityState.Show;
                else this.explorer.DetailsPane = PaneVisibilityState.Hide;
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
            if (this.WindowState == WindowState.Maximized)
            {
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
}
