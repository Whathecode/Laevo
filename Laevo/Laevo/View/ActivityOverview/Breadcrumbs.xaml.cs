using System.Windows;
using System.Windows.Controls;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview;


namespace Laevo.View.ActivityOverview
{
	/// <summary>
	/// Interaction logic for Breadcrumbs.xaml
	/// </summary>
	public partial class Breadcrumbs
	{
		public Breadcrumbs()
		{
			InitializeComponent();
		}


		void OnActivityDrop( object sender, DragEventArgs e )
		{
			var activity = (ActivityViewModel)e.Data.GetData( typeof( ActivityViewModel ) );
			var button = (Button)sender;
			var parentActivity = (ActivityViewModel)button.DataContext;
			var overview = (ActivityOverviewViewModel)DataContext;

			overview.MoveActivity( activity, parentActivity );
		}
	}
}