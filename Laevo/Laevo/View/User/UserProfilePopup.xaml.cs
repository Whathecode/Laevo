﻿using System;
using System.Windows;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;


namespace Laevo.View.User
{
	/// <summary>
	///   Interaction logic for UserProfilePopup.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	public partial class UserProfilePopup
	{
		[Flags]
		public enum Properties
		{
		}


		public UserProfilePopup()
		{
			InitializeComponent();
		}

		void OnSaveButtonClicked( object sender, RoutedEventArgs e )
		{
			Close();
		}
	}
}
