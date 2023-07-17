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
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack;
using System.IO;

namespace Archiver
{
    /// <summary>
    /// Interaction logic for Extract.xaml
    /// </summary>
    public partial class Compress : Window
    {
        public Compress()
        {
            InitializeComponent();

            this.selectSource.Click += (s, e) => {
                FileSelector selector = new FileSelector();
                selector.ShowDialog();
                if (selector.DialogResult == true) {
                    
                }
            };

            this.selectWildMask.Click += (s, e) => {
                SystemWildcardSelect selector = new SystemWildcardSelect();
                selector.ShowDialog();
                if (selector.DialogResult == true) {

                }
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
    }

    public class FileSystemTree : SystemDirectory
    {
    }
    
    public abstract class SystemItem 
    {
        public string FullName { get; set; }
        public string DisplayName { get; set; }
    }

    public class SystemFile : SystemItem
    {
        public string Extension { get; set; }
    }

    public class SystemDirectory : SystemItem
    {
        public List<SystemFile> Files { get; set; } = new List<SystemFile>();
        public List<SystemDirectory> Directories { get; set; } = new List<SystemDirectory>();
    }
}
