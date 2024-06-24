using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BreadcrumbLib
{
	static class Helper
	{
		public static TChildItem FindVisualChild<TChildItem>(DependencyObject obj) where TChildItem : DependencyObject
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(obj, i);
				if (child != null && child is TChildItem)
					return (TChildItem)child;
				else
				{
					TChildItem childOfTchild = FindVisualChild<TChildItem>(child);
					if (childOfTchild != null)
						return childOfTchild;
				}
			}
			return null;
		}

		public static TParentItem FindVisualParent<TParentItem>(DependencyObject obj) where TParentItem : DependencyObject
		{
			DependencyObject current = obj;
			while (current != null && !(current is TParentItem))
				current = VisualTreeHelper.GetParent(current);
			return (TParentItem)current;
		}

		public static DataTemplateSelector GetDefaultSelector()
		{
			Type type = typeof(ContentPresenter).Assembly.GetType("System.Windows.Controls.ContentPresenter+DefaultSelector");
			ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
			DataTemplateSelector defaultSelector = (DataTemplateSelector)constructor.Invoke(null);
			return defaultSelector;
		}
	}
}
