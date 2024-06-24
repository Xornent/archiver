using Archiver;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using static Vanara.PInvoke.ComCtl32;

namespace BreadcrumbLib
{
	[TemplatePart(Type = typeof(ItemsControl), Name = PartNameView)]
	public class Breadcrumb : ItemsControl
	{
		protected const string PartNameView = "PART_View";

		protected static readonly DependencyPropertyKey SelectedItemPropertyKey = DependencyProperty.RegisterReadOnly(
			"SelectedItem", typeof(object), typeof(Breadcrumb), new FrameworkPropertyMetadata(null,
				FrameworkPropertyMetadataOptions.AffectsRender));
		public static readonly DependencyProperty SelectedItemProperty = SelectedItemPropertyKey.DependencyProperty;

		public static readonly DependencyProperty ButtonsProperty = DependencyProperty.Register("Buttons",
			typeof(ObservableCollection<ButtonBase>), typeof(Breadcrumb), new UIPropertyMetadata(null));

		private ItemsControl view;

		static Breadcrumb()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Breadcrumb), new FrameworkPropertyMetadata(typeof(Breadcrumb)));
		}

		public object SelectedItem
		{
			get { return GetValue(SelectedItemProperty); }
			private set { SetValue(SelectedItemPropertyKey, value); }
		}

		public ObservableCollection<ButtonBase> Buttons
		{
			get { return (ObservableCollection<ButtonBase>)GetValue(ButtonsProperty); }
			set { SetValue(ButtonsProperty, value); }
		}

		public Breadcrumb()
		{
			Buttons = new ObservableCollection<ButtonBase>();
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			view = GetTemplateChild(PartNameView) as ItemsControl;

			if (!Items.IsEmpty)
				GoTo(Items[0]);
		}

		///<summary>
		///Invoked when the <see cref="P:System.Windows.Controls.ItemsControl.Items" /> property changes.
		///</summary>
		///
		///<returns>
		///
		///</returns>
		///
		///<param name="e">Information about the change.</param>
		protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			if(view != null && view.Items.IsEmpty && !Items.IsEmpty)
				GoTo(Items[0]);
			base.OnItemsChanged(e);
		}

		internal void AddTrail(object parent, object item)
		{
			if (parent == item)
				return;

			int index = 0;

			ItemCollection items = view.Items;
			if (parent != null)
				index = GetIndex(parent, items);

			for (int i = items.Count - 1; i >= index + 1; i--)
				RemoveItem(items, i);

			AddAndSelect(item, items);
		}

		internal void GoTo(object target)
		{
			ItemCollection items = view.Items;
			int index = GetIndex(target, items);

			for (int i = items.Count - 1; i >= index; i--)
				RemoveItem(items, i);

			AddAndSelect(target, items);
		}

        internal void Navigate(object target)
        {
            BreadcrumbItem? arch = view.Items[0] as BreadcrumbItem;
			if (arch == null) return;

			List<object> cascade = new List<object>();
			if (target is FolderItem folder) cascade.Add(target);
			if (target is FileSystemNode node)
			{
				while(node.Parent != null)
				{
					cascade.Insert(0, node.Parent);
					node = node.Parent;
				}
			}

			// only reserve the root item.
			if(cascade.Count <= 1)
			{
				if (view.Items.Count > 1)
				{
					for (int i = 1; i < view.Items.Count; i++)
						view.Items.RemoveAt(1);
				}

				BreadcrumbItem? root = view.Items[0] as BreadcrumbItem;
				if (root != null) root.IsSelected = true;
				this.SelectedItem = target is Arch ? target : cascade[0];
				return;
			}

            // the first of a cascade is always arch. now remove that
            cascade.RemoveAt(0);

            BreadcrumbItem? r = view.Items[0] as BreadcrumbItem;
			if (r == null) return;
            this.AddTrail(r, cascade[0]);
            for(int i = 1; i < cascade.Count; i++)
			{
				this.AddTrail(cascade[i - 1], cascade[i]);
				this.SelectedItem = cascade[i];

				// here we do not invoke the selection changed event.
				// because this is manually driven.
			}
        }

        private void AddAndSelect(object item, IList items)
		{
			if(Items.Contains(item))
				Items.Remove(item);
			items.Add(item);
			SelectedItem = item;

			BreadcrumbItem container = view.ItemContainerGenerator.ContainerFromItem(item) as BreadcrumbItem;
			if (container != null)
				container.IsSelected = true;
		}

		private static int GetIndex(object item, IList items)
		{
			int index = 0;
			for (int i = items.Count - 1; i >= 0; i--)
			{
				if (items[i].Equals(item))
				{
					index = i;
					break;
				}
			}
			return index;
		}

		private void RemoveItem(IList items, int i)
		{
			BreadcrumbItem container = view.ItemContainerGenerator.ContainerFromIndex(i) as BreadcrumbItem;
			if (container != null)
				container.IsSelected = false;
			items.RemoveAt(i);
		}

		public event EventHandler SelectionChanged;

		internal void InvokeSelectionChanged(object sender, EventArgs e)
		{
			this.SelectionChanged?.Invoke(sender, e);
		}
	}
}