using System;
using System.Windows;
using System.Windows.Input;
using Laevo.ViewModel.Activity;
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
			WorkIntervalDataContext = 1,
			IsIntervalPast
		}


		[DependencyProperty( Properties.WorkIntervalDataContext, DefaultValue = null )]
		public WorkIntervalViewModel WorkIntervalDataContext { get; set; }

		[DependencyProperty( Properties.IsIntervalPast )]
		public bool IsIntervalPast { get; private set; }


		public ActionButtons()
		{
			InitializeComponent();

			IsVisibleChanged += ( sender, args ) =>
			{
				if ( WorkIntervalDataContext != null )
				{
					IsIntervalPast = WorkIntervalDataContext.IsPast();
				}
			};
		}
		

		void SetFocus( object sender, MouseButtonEventArgs e )
		{
			((UIElement)e.Source).Focus();
		}
	}
}
