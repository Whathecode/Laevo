﻿using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Laevo.View.ActivityBar
{
	/// <summary>
	/// Interaction logic for ActivityMenu.xaml
	/// </summary>
	public partial class ActivityMenu
	{
		public ActivityMenu()
		{
			InitializeComponent();
		}


		/// <summary>
		/// Resets properties and shows other activity states menu.
		/// </summary>
		void ShowOtherStatesMenu( object sender, RoutedEventArgs e )
		{
			OtherStatesButton.ContextMenu.Visibility = Visibility.Visible;
			OtherStatesButton.ContextMenu.PlacementTarget = OtherStatesButton;
			OtherStatesButton.ContextMenu.Placement = PlacementMode.Right;
			OtherStatesButton.ContextMenu.Focus();
			OtherStatesButton.ContextMenu.IsOpen = true;
		}

		/// <summary>
		/// Hides other activity states menu.
		/// </summary>
		void HidetherStatesMenu( object sender, EventArgs e )
		{
			OtherStatesButton.ContextMenu.Visibility = Visibility.Hidden;
			OtherStatesButton.ContextMenu.IsOpen = false;
		}
	}
}
