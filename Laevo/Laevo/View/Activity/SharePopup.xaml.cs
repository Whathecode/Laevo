using System;
using System.Windows;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;


namespace Laevo.View.Activity
{
	/// <summary>
	///   Interaction logic for SharePopup.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	public partial class SharePopup
	{
		[Flags]
		public enum Properties
		{
		}


		public SharePopup()
		{
			InitializeComponent();
		}

		void OnCloseButtonClicked( object sender, RoutedEventArgs e )
		{
			Close();
		}
	}
}
