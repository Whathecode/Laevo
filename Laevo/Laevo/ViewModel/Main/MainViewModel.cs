using System.Windows;
using Laevo.View.ActivityOverview;
using Laevo.ViewModel.ActivityOverview;
using Laevo.ViewModel.Main.Binding;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.Main
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class MainViewModel
	{
		ActivityOverviewWindow _activityOverview;
		ActivityOverviewViewModel _activityOverviewViewModel;


		public MainViewModel()
		{
			EnsureActivityOverview();
		}


		[CommandExecute( Commands.Exit )]
		public void Exit()
		{
			Application.Current.Shutdown();
		}

		[CommandExecute( Commands.ShowActivityOverview )]
		public void ShowActivityOverview()
		{
			EnsureActivityOverview();

			_activityOverview.Show();
		}

		[CommandExecute( Commands.HideActivityOverview )]
		public void HideActivityOverview()
		{
			_activityOverview.Hide();
		}

		/// <summary>
		///   Ensure that the activity overview window is created.
		/// </summary>
		void EnsureActivityOverview()
		{
			if ( _activityOverview != null && _activityOverview.IsLoaded )
			{
				return;
			}

			_activityOverviewViewModel = new ActivityOverviewViewModel();
			_activityOverviewViewModel.OpenedActivityEvent += ShowActivityOverview;
			_activityOverview = new ActivityOverviewWindow
			{
				DataContext = _activityOverviewViewModel
			};
		}
	}
}
