using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static Vanara.PInvoke.ComCtl32;

namespace BreadcrumbLib
{
	internal class BreadcrumbButton : Button
	{
		private static RoutedUICommand gotoCommand = new RoutedUICommand("Go to", "GoToCommand", typeof(BreadcrumbButton));
		private static RoutedUICommand invokeMenuCommand = new RoutedUICommand("Open menu", "InvokeMenuCommand", typeof(BreadcrumbButton));
		private ContextMenu menu;

		public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register("IsExpanded", typeof(bool),
			typeof(BreadcrumbButton), new UIPropertyMetadata(false));

		private ToggleButton buttonExpand;
		private Button button;

		static BreadcrumbButton()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BreadcrumbButton), new FrameworkPropertyMetadata(typeof(BreadcrumbButton)));
		}

		public BreadcrumbButton()
		{
            CommandBindings.Add(new CommandBinding(GoToCommand, CommandBindingToButton_Executed));
		}

		public bool IsExpanded
		{
			get { return (bool)GetValue(IsExpandedProperty); }
			set { SetValue(IsExpandedProperty, value); }
		}

		public static RoutedUICommand GoToCommand
		{
			get { return gotoCommand; }
		}

		public static RoutedUICommand InvokeMenuCommand
		{
			get { return invokeMenuCommand; }
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			buttonExpand = (ToggleButton)GetTemplateChild("buttonExpand");
			button = (Button)GetTemplateChild("button");

			menu = (ContextMenu)GetTemplateChild("menu");
			if (menu != null)
			{
				menu.DataContext = DataContext;
				menu.Opened += menu_Opened;

                // by the first time it's opened, the Opened method is not
                // called and results in wrong display. so we force the opened 
                // function to be called.
                
				menu.Opacity = 0;
                menu.IsOpen = true;
				menu.IsOpen = false;

                BreadcrumbItem templatedParent = TemplatedParent as BreadcrumbItem;
				if (templatedParent != null && templatedParent.Items.Count > 0)
				{
					DataTemplateSelector selector = Helper.GetDefaultSelector();
					DataTemplate template = selector.SelectTemplate(templatedParent.Items[0], this);
					menu.ItemTemplate = template;
				}

				menu.CommandBindings.Add(new CommandBinding(InvokeMenuCommand, CommandBindingToMenuItem_Executed));
			}
			DependencyPropertyDescriptor isPressedProperty = DependencyPropertyDescriptor.FromProperty(IsPressedProperty, typeof(Button));
			isPressedProperty.AddValueChanged(button, OnIsPressedChangedIntern);
		}

		private BreadcrumbView View
		{
			get { return Helper.FindVisualParent<BreadcrumbView>(this); }
		}

		void menu_Opened(object sender, RoutedEventArgs e)
		{
            menu.Opacity = 1;
            BreadcrumbView view = View;

			BreadcrumbItem templatedParent = TemplatedParent as BreadcrumbItem;
			
			menu.Placement = PlacementMode.Relative;
			menu.PlacementTarget = buttonExpand;
			menu.VerticalOffset = buttonExpand.ActualHeight;
			menu.HorizontalOffset = buttonExpand.ActualWidth + menu.BorderThickness.Left - 5;

			if ((templatedParent != null) && templatedParent.IsFirst)
			{
				Image image = Helper.FindVisualChild<Image>(templatedParent);
				if (image != null)
					menu.HorizontalOffset -= image.ActualWidth;
			}

			if (view != null)
			{
				int index = view.ItemContainerGenerator.IndexFromContainer(TemplatedParent);
				if (index >= 0)
				{
					menu.UpdateLayout();
					TextBlock menuText = Helper.FindVisualChild<TextBlock>(menu);
					if (menuText != null)
					{
						double menuTextOffset = menuText.TransformToVisual(menu).Transform(new Point(0, 0)).X;
						TextBlock buttonText = Helper.FindVisualChild<TextBlock>(button);
						if (buttonText != null)
						{
							double buttonTextOffset = buttonText.TransformToVisual((Visual)TemplatedParent).Transform(new Point(0, 0)).X;
							menu.HorizontalOffset -= (menuTextOffset - buttonTextOffset);
						}
					}
				}
			}

			foreach (object o in menu.Items)
			{
				MenuItem menuItem = menu.ItemContainerGenerator.ContainerFromItem(o) as MenuItem;
				if(menuItem != null)
				{
					FieldInfo rolePropertyKeyField = typeof(MenuItem).GetField("RolePropertyKey",
						BindingFlags.NonPublic | BindingFlags.Static);
					DependencyPropertyKey rolePropertyKey = (DependencyPropertyKey)rolePropertyKeyField.GetValue(menuItem);
					menuItem.SetValue(rolePropertyKey, MenuItemRole.TopLevelItem);

					BreadcrumbItem container = view.ItemContainerGenerator.ContainerFromItem(menuItem.DataContext) as BreadcrumbItem;
					if (container != null && container.IsSelected)
						menuItem.FontWeight = FontWeights.Bold;
					else
						menuItem.FontWeight = FontWeights.Normal;
				}
			}
		}

		protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
		{
			e.Handled = true;
			base.OnPreviewMouseRightButtonUp(e);
		}

		private void CommandBindingToMenuItem_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			FrameworkElement itemsControl = DataContext as FrameworkElement;
			if (itemsControl != null)
			{
				Breadcrumb parent = Helper.FindVisualParent<Breadcrumb>(this);
				parent.AddTrail(itemsControl.DataContext, e.Parameter);
                parent.InvokeSelectionChanged(sender, e);
            }
		}

		private void CommandBindingToButton_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			FrameworkElement itemsControl = DataContext as FrameworkElement;
			if (itemsControl != null)
			{
				Breadcrumb parent = Helper.FindVisualParent<Breadcrumb>(this);
				object target = itemsControl.DataContext;
				if (target == null)
					target = itemsControl;
				parent.GoTo(target);

				parent.InvokeSelectionChanged(sender, e);
			}
		}

		private void OnIsPressedChangedIntern(object sender, EventArgs e)
		{
			if (buttonExpand != null)
				typeof(ButtonBase).GetProperty("IsPressed").SetValue(buttonExpand, button.IsPressed, null);
		}
	}
}