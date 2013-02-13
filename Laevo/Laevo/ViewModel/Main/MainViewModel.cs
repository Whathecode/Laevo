using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using Laevo.View.ActivityOverview;
using Laevo.View.Settings;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview;
using Laevo.ViewModel.ActivityOverview.Binding;
using Laevo.ViewModel.Settings;
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
		readonly Dispatcher _dispatcher;


		public MainViewModel( Model.Laevo model )
		{			
			_model = model;
			_dispatcher = Dispatcher.CurrentDispatcher;
			_model.LogonScreenExited += () => _dispatcher.Invoke( new Action( ResetGui ) );

			EnsureActivityOverview();
		}


		/// <summary>
		///   HACK: This functionality is provided since this is still a prototype and sometimes the GUI seems to hang.
		///         This could be due to a possible WPF bug:
		///			https://connect.microsoft.com/VisualStudio/feedback/details/602232/when-using-cachemode-bitmapcache-upon-waking-up-from-sleep-wpf-rendering-thread-consumes-40-cpu#tabs
		/// </summary>
		void ResetGui()
		{
			if ( _activityOverview != null )
			{
				_activityOverview.Close();
				_activityOverview = null;
				if ( _activityOverviewViewModel.ActivityMode == Mode.Select )
				{
					ShowActivityOverview();
				}
			}
		}


		[CommandExecute( Commands.Exit )]
		public void Exit()
		{
			Persist();
			_model.Exit();

			Application.Current.Shutdown();
		}

		[CommandExecute( Commands.OpenSettings )]
		public void OpenSettings()
		{
			var viewModel = new SettingsViewModel( _model.Settings );
			var settingsWindow = new SettingsWindow
			{
				DataContext = viewModel
			};

			settingsWindow.Closed += ( s, a ) =>
			{
				viewModel.Persist();
				_activityOverviewViewModel.TimeLineRenderScale = viewModel.TimeLineRenderScale;
				_activityOverviewViewModel.EnableAttentionLines = viewModel.EnableAttentionLines;
			};
			
			settingsWindow.Show();
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
				_activityOverviewViewModel.ActivityMode = Mode.Activate;
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
			EnsureActivityOverview();

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
			return _activityOverviewViewModel.ActivityMode == Mode.Activate;
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

		[CommandExecute( Commands.CutWindow )]
		public void CutWindow()
		{
			_activityOverviewViewModel.CutWindow();
		}

		[CommandExecute( Commands.PasteWindows )]
		public void PasteWindows()
		{
			_activityOverviewViewModel.PasteWindows();
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

			if ( _activityOverviewViewModel == null )
			{
				_activityOverviewViewModel = new ActivityOverviewViewModel( _model )
				{
					TimeLineRenderScale = _model.Settings.TimeLineRenderAtScale,
					EnableAttentionLines = _model.Settings.EnableAttentionLines
				};
			}
			_activityOverviewViewModel.ActivatedActivityEvent += OnActivatedActivityEvent;
			_activityOverviewViewModel.ClosedActivityEvent += OnClosedActivityEvent;
			_activityOverview = new ActivityOverviewWindow
			{
				DataContext = _activityOverviewViewModel
			};
		}

		void OnActivatedActivityEvent( ActivityViewModel viewModel )
		{
			HideActivityOverview();
		}

		void OnClosedActivityEvent( ActivityViewModel viewModel )
		{
			// Open time line in order to select a new activity to continue work on.
			SelectActivity( a => a.ActivateActivity() );
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
