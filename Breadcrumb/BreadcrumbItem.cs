using System;
using System.Windows;
using System.Windows.Controls;

namespace BreadcrumbLib
{
	public class BreadcrumbItem : HeaderedItemsControl
	{
		public static readonly DependencyProperty IsExpandedProperty =
			DependencyProperty.Register("IsExpanded", typeof(bool), typeof(BreadcrumbItem), new UIPropertyMetadata(false));

		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register("IsSelected", typeof(bool), typeof(BreadcrumbItem), new UIPropertyMetadata(false));

		public static readonly DependencyProperty ImageProperty =
			DependencyProperty.Register("Image", typeof(object), typeof(BreadcrumbItem), new UIPropertyMetadata(null));

		public static readonly DependencyProperty ParentItemProperty =
			DependencyProperty.Register("ParentItem", typeof(object), typeof(BreadcrumbItem), new UIPropertyMetadata(null));

		public static readonly DependencyProperty IsFirstProperty = DependencyProperty.Register("IsFirst", typeof(bool),
			typeof(BreadcrumbButton), new UIPropertyMetadata(false));

		internal static readonly DependencyProperty ViewProperty =
			DependencyProperty.Register("View", typeof(BreadcrumbView), typeof(BreadcrumbItem), new UIPropertyMetadata(null));

		internal BreadcrumbView View
		{
			get { return (BreadcrumbView)GetValue(ViewProperty); }
			set { SetValue(ViewProperty, value); }
		}

		public bool IsFirst
		{
			get { return (bool)GetValue(IsFirstProperty); }
			set { SetValue(IsFirstProperty, value); }
		}

		public object ParentItem
		{
			get { return GetValue(ParentItemProperty); }
			set { SetValue(ParentItemProperty, value); }
		}

		public object Image
		{
			get { return GetValue(ImageProperty); }
			set { SetValue(ImageProperty, value); }
		}

		public bool IsSelected
		{
			get { return (bool)GetValue(IsSelectedProperty); }
			set { SetValue(IsSelectedProperty, value); }
		}

		public bool IsExpanded
		{
			get { return (bool)GetValue(IsExpandedProperty); }
			set { SetValue(IsExpandedProperty, value); }
		}

		static BreadcrumbItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BreadcrumbItem), new FrameworkPropertyMetadata(typeof(BreadcrumbItem)));
		}

		public BreadcrumbItem()
			: this(null, null)
		{ }

		internal BreadcrumbItem(object parent, BreadcrumbView view)
		{
			View = view;
			ParentItem = parent;
			IsFirst = ParentItem == null;
		}
	}
}