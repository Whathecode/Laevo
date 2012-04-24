using System;
using System.Reactive.Linq;
using System.Windows;
using Laevo.View.ActivityOverview;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview;
using Laevo.ViewModel.ActivityOverview.Binding;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;
using Application = System.Windows.Application;
using Commands = Laevo.ViewModel.Main.Binding.Commands;


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
			Persist();
			_model.Exit();

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
			_activityOverviewViewModel.ActivityMode = Mode.Select;
			var awaitOpen = Observable.FromEvent<ActivityViewModel.ActivityEventHandler, ActivityViewModel>(
				h => _activityOverviewViewModel.SelectedActivityEvent += h,
				h => _activityOverviewViewModel.SelectedActivityEvent -= h );
			awaitOpen.Subscribe( a =>
			{
				selectedActivity( a );
				_activityOverviewViewModel.ActivityMode = Mode.Open;
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
			return _activityOverviewViewModel.ActivityMode == Mode.Open;
		}

		[CommandExecute( Commands.OpenCurrentActivityLibrary )]
		public void OpenCurrentActivityLibrary()
		{
			_activityOverviewViewModel.CurrentActivityViewModel.OpenActivityLibrary();
		}

		[CommandExecute( Commands.CloseActivity )]
		public void CloseActivity()
		{
			if ( _activityOverviewViewModel.CurrentActivityViewModel != null )
			{
				_activityOverviewViewModel.CurrentActivityViewModel.CloseActivity();
			}
		}

		[CommandCanExecute( Commands.CloseActivity )]
		public bool CanCloseActivity()
		{
			return _activityOverviewViewModel.CurrentActivityViewModel != null && _activityOverviewViewModel.CurrentActivityViewModel.CanCloseActivity();
		}

		[CommandExecute( Commands.NewActivity )]
		public void NewActivity()
		{
			_activityOverviewViewModel.NewActivity();
		}

		[CommandCanExecute( Commands.NewActivity )]
		public bool CanNewActivity()
		{
			return _activityOverviewViewModel.ActivityMode != Mode.Select;
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
			_activityOverviewViewModel.ClosedActivityEvent += OnClosedActivityEvent;
			_activityOverview = new ActivityOverviewWindow
			{
				DataContext = _activityOverviewViewModel
			};
		}

		void OnOpenedActivityEvent( ActivityViewModel viewModel )
		{
			HideActivityOverview();
		}

		void OnClosedActivityEvent( ActivityViewModel viewModel )
		{
			// Open time line in order to select a new activity to continue work on.
			SelectActivity( a => a.OpenActivity() );
		}

		public override void Persist()
		{
			_activityOverviewViewModel.Persist();
		}

		protected override void FreeUnmanagedResources()
		{
			_activityOverviewViewModel.Dispose();
		}
	}
}
