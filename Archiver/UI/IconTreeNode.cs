using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Archiver.UI
{
    internal class IconTreeNode : INotifyPropertyChanged
    {
        public ImageSource Icon { get; set; }

        public string Caption { get; set; }

        public string Prefix { get; set; } = "";

        private ObservableCollection<IconTreeNode> _items = new ObservableCollection<IconTreeNode>();
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<IconTreeNode> Items
        {
            get { return _items; }
            set {
                if (_items == value)
                    return;
                _items = value;
                PropertyChanged?.Invoke("Items", null);
            }
        }
    }
}
