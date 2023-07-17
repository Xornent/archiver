﻿using Microsoft.WindowsAPICodePack.Shell;
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
    /// <summary>
    /// Interaction logic for FileSelector.xaml
    /// </summary>
    public partial class FileSelector : Window
    {
        public FileSelector()
        {
            InitializeComponent();
            this.explorer.NavigationTarget = ShellObject.FromParsingName("C:");
            this.explorer.SelectedItems.CollectionChanged += (s, e) => {
                this.SelectedDirectories.Clear();
                this.SelectedFiles.Clear();

                foreach (var item in this.explorer.SelectedItems) {
                    if (item.IsFileSystemObject) {
                        if (item is ShellFile file) {
                            this.SelectedFiles.Add(new FileInfo(file.ParsingName));
                        } else if (item is ShellFolder folder) {
                            this.SelectedDirectories.Add(new DirectoryInfo(folder.ParsingName));
                        }
                    }
                }
            };

            this.btnCancel.Click += (s, e) => {
                this.DialogResult = false;
                this.Close();
            };

            this.btnOK.Click += (s, e) => {
                this.DialogResult = true;
                this.Close();
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

        public List<FileInfo> SelectedFiles { get; private set; } = new List<FileInfo>();
        public List<DirectoryInfo> SelectedDirectories { get; private set; } = new List<DirectoryInfo>();
    }
}
