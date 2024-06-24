using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    /// Interaction logic for Extract.xaml
    /// </summary>
    public partial class Find : Window
    {
        public Find(Archiver.Arch archive, bool isSPF)
        {
            InitializeComponent();
            if (isSPF) {
                this.btnCut.IsEnabled = false;
                this.btnDelete.IsEnabled = false;
            }

            // visit all available items.
            void visitDirectory(FileSystemNode node)
            {
                if(node is Arch a) {
                    foreach (var item in a.Children) {
                        if (item is FileItem f)
                            this.items.Add(f);
                        else if (item is FolderItem fd)
                            visitDirectory(fd);
                    }
                } else if(node is FolderItem fd2) {
                    foreach (var item in fd2.Children) {
                        if (item is FileItem f)
                            this.items.Add(f);
                        else if (item is FolderItem fd)
                            visitDirectory(fd);
                    }
                }
            }

            visitDirectory(archive);
            foreach (var item in this.items) {
                this.list.Items.Add(item);
            }

            this.txtFind.TextChanged += (s, e) => {
                this.list.Items.Clear();

                if (string.IsNullOrEmpty(this.txtFind.Text)) {
                    foreach (var item in this.items) {
                            this.list.Items.Add(item);
                    }
                } else foreach (var item in this.items) {
                    if (item.FullName.Contains(this.txtFind.Text))
                        this.list.Items.Add(item);
                }
            };

            RoutedEventHandler delDeleteSelection = (s, e) => {
                if (archive == null) return;
            };

            RoutedEventHandler delCopy = (s, e) => {
                if (archive == null) return;
            };

            RoutedEventHandler delCut = (s, e) => {
                delCopy(s, e);
                delDeleteSelection(s, e);
            };

            this.btnCopy.Click += (s, e) => { delCopy(s, e); this.Close(); };
            this.btnCut.Click += (s, e) => { delCut(s, e); this.Close(); };
            this.btnDelete.Click += (s, e) => { delDeleteSelection(s, e); this.Close(); };
            this.btnOK.Click += (s, e) => { this.Close(); };
        }

        List<Item> items = new List<Item>();

        private void titleBarDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void windowClose(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void windowStateChange(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
                return;
            }
            this.WindowState = WindowState.Minimized;
        }
    }
}
