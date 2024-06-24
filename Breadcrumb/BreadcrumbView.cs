using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BreadcrumbLib
{
	internal class BreadcrumbView : ItemsControl
	{
		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
			"SelectedItem", typeof(BreadcrumbItem), typeof(BreadcrumbView), new UIPropertyMetadata(null));

		public BreadcrumbView()
		{
			ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
		}

		public BreadcrumbItem SelectedItem
		{
			get { return (BreadcrumbItem)GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			object parent = null;
			if (Items.Count > 1)
				parent = Items[Items.Count - 2];
			return new BreadcrumbItem(parent, this);
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is BreadcrumbItem;
		}

		private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
		{
			if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
			{
				if (!Items.IsEmpty)
				{
					object item = Items[Items.Count - 1];
					DependencyObject container = ItemContainerGenerator.ContainerFromItem(item);
					SelectedItem = (BreadcrumbItem)container;
				}
			}
		}
	}
}