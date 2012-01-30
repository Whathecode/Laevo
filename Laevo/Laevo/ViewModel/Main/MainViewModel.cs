using System.Windows;
using Laevo.View.ActivityOverview;
using Laevo.ViewModel.ActivityOverview;
using Laevo.ViewModel.Main.Binding;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.Main
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class MainViewModel
	{
		readonly ActivityOverviewWindow _activityOverview;


		public MainViewModel()
		{
			var activityOverviewViewModel = new ActivityOverviewViewModel();
			_activityOverview = new ActivityOverviewWindow
			{
			    DataContext = activityOverviewViewModel
			};
		}


		[CommandExecute( Commands.Exit )]
		public void Exit()
		{
			Application.Current.Shutdown();
		}

		[CommandExecute( Commands.OpenTimeLine )]
		public void OpenTimeLine()
		{
			if ( _activityOverview.Visibility.EqualsAny( Visibility.Collapsed, Visibility.Hidden ) )
			{
				_activityOverview.Show();
			}
			else
			{
				_activityOverview.Hide();
			}
		}
	}
}
