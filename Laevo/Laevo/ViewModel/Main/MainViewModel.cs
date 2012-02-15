using System.Windows;
using Laevo.View.ActivityOverview;
using Laevo.ViewModel.Activity;
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
		Model.Laevo _model;
		ActivityOverviewWindow _activityOverview;
		ActivityOverviewViewModel _activityOverviewViewModel;


		public MainViewModel( Model.Laevo model )
		{
			_model = model;

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

		[CommandExecute( Commands.SwitchActivityOverview )]
		public void SwitchActivityOverview()
		{
			if ( _activityOverview.Visibility.EqualsAny( Visibility.Collapsed, Visibility.Hidden ) )
			{
				ShowActivityOverview();
			}
			else
			{
				HideActivityOverview();
			}
		}

		[CommandExecute( Commands.OpenCurrentActivityLibrary )]
		public void OpenCurrentActivityLibrary()
		{
			_activityOverviewViewModel.CurrentActivityViewModel.OpenActivityLibrary();
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

			_activityOverviewViewModel = new ActivityOverviewViewModel( _model );
			_activityOverviewViewModel.OpenedActivityEvent += OnOpenedActivityEvent;
			_activityOverview = new ActivityOverviewWindow
			{
				DataContext = _activityOverviewViewModel
			};
		}

		void OnOpenedActivityEvent( ActivityViewModel viewModel )
		{
			HideActivityOverview();
		}
	}
}
