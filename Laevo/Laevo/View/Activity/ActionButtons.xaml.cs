﻿using System;
using System.Windows.Input;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;


namespace Laevo.View.Activity
{
	/// <summary>
	/// Interaction logic for ActionButtons.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	public partial class ActionButtons
	{
		[Flags]
		public enum Properties
		{
			ShowStartButton
		}


		[DependencyProperty( Properties.ShowStartButton, DefaultValue = true)]
		public bool ShowStartButton { get; set; }


		public ActionButtons()
		{
			InitializeComponent();
		}


		void OnStartEditActivity( object sender, MouseButtonEventArgs e )
		{
			Common.ForceUpdateSource( e );
		}
	}
}