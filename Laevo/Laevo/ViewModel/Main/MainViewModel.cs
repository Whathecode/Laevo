using System;
using System.Reactive.Linq;
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
	class MainViewModel : AbstractViewModel
	{
		readonly Model.Laevo _model;
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

		/// <summary>
		///   Opens the activity overview in order to select one of the activities.
		/// </summary>
		/// <param name="selectedActivity">The action to perform on the selected activity.</param>
		public void SelectActivity( Action<ActivityViewModel> selectedActivity )
		{
			_activityOverviewViewModel.ActivityMode = ActivityOverviewViewModel.Mode.Select;
			var awaitOpen = Observable.FromEvent<ActivityViewModel.ActivityEventHandler, ActivityViewModel>(
				h => _activityOverviewViewModel.SelectedActivityEvent += h,
				h => _activityOverviewViewModel.SelectedActivityEvent -= h );
			awaitOpen.Subscribe( a =>
			{
				selectedActivity( a );
				_activityOverviewViewModel.ActivityMode = ActivityOverviewViewModel.Mode.Open;
				HideActivityOverview();
			} );
			ShowActivityOverview();
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

		[CommandCanExecute( Commands.SwitchActivityOverview )]
		public bool CanSwitchActivityOverview()
		{
			return _activityOverviewViewModel.ActivityMode != ActivityOverviewViewModel.Mode.Select;
		}

		[CommandExecute( Commands.OpenCurrentActivityLibrary )]
		public void OpenCurrentActivityLibrary()
		{
			_activityOverviewViewModel.CurrentActivityViewModel.OpenActivityLibrary();
		}

		[CommandExecute( Commands.NewActivity )]
		public void NewActivity()
		{
			_activityOverviewViewModel.NewActivity();
		}

		/// <summary>
		///   Ensure that the activity overview window is created.
		/// </summary>
		void EnsureActivityOverview()
		{
			if ( _activityOverview != null )
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

		public override void Persist()
		{
			_activityOverviewViewModel.Persist();
		}
	}
}
