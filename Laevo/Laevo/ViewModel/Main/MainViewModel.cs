using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using Laevo.View.ActivityOverview;
using Laevo.View.Settings;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview;
using Laevo.ViewModel.ActivityOverview.Binding;
using Laevo.ViewModel.Settings;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
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

		readonly Queue<ActivityViewModel> _lastActivatedActivities = new Queue<ActivityViewModel>();

		public event Action GuiReset;

		[NotifyProperty( Binding.Properties.UnattendedInterruptions )]
		public int UnattendedInterruptions { get; private set; }


		public MainViewModel( Model.Laevo model )
		{			
			_model = model;
			_dispatcher = Dispatcher.CurrentDispatcher;
			_model.LogonScreenExited += () => _dispatcher.Invoke( ResetGui );

			_model.InterruptionAdded += a =>
			{
				UnattendedInterruptions++;
			};
			_model.ActivityRemoved += a => UpdateUnattendedInterruptions();			

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

			GuiReset();
		}

		void OnActivityActivated( Model.Activity activity )
		{
			activity.ActivatedEvent -= OnActivityActivated;
			UpdateUnattendedInterruptions();
		}

		void UpdateUnattendedInterruptions()
		{
			UnattendedInterruptions = _model.Activities.Concat( _model.Tasks ).Sum( a => a.Interruptions.Count( i => !i.AttendedTo ) );
		}

		[CommandExecute( Commands.Exit )]
		public void Exit()
		{
			_activityOverviewViewModel.Exit();
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
			_activityOverview.Activate();
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
				h => _activityOverviewViewModel.SelectedActivityEvent -= h ).Take( 1 );
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
			return _activityOverviewViewModel.CurrentActivityViewModel != null;
		}

		[CommandExecute( Commands.NewActivity )]
		public void NewActivity()
		{
			ActivityViewModel activity = _activityOverviewViewModel.CreateNewActivity();
			activity.ActivateActivity();
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

		[CommandExecute( Commands.SwitchActivity )]
		public void SwitchActivity()
		{
			if ( _lastActivatedActivities.Count < 2 )
			{
				return;
			}

			ActivityViewModel lastActivity = _lastActivatedActivities.Dequeue();
			lastActivity.ActivateActivity( lastActivity.IsOpen );
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
				_lastActivatedActivities.Enqueue( _activityOverviewViewModel.HomeActivity );
				_activityOverviewViewModel.ActivatedActivityEvent += OnActivatedActivityEvent;
				_activityOverviewViewModel.NoCurrentActiveActivityEvent += OnNoCurrentActiveActivityEvent;
				_activityOverviewViewModel.ActivatedActivityEvent += ( activity, newActivity ) => UpdateUnattendedInterruptions();
			}
			_activityOverview = new ActivityOverviewWindow
			{
				DataContext = _activityOverviewViewModel
			};
			_activityOverview.Activated += ( sender, args ) => _activityOverviewViewModel.OnOverviewActivated();
		}
		
		void OnActivatedActivityEvent( ActivityViewModel oldActivity, ActivityViewModel newActivity )
		{
			if ( oldActivity != newActivity )
			{
				// Keep track of last 2 actived activities.
				_lastActivatedActivities.Enqueue( newActivity );
				if ( _lastActivatedActivities.Count > 2 )
				{
					_lastActivatedActivities.Dequeue();
				}
			}

			HideActivityOverview();
		}

		void OnNoCurrentActiveActivityEvent()
		{
			// Open time line in order to select a new activity to continue work on.
			SelectActivity( a => a.ActivateActivity( a.IsOpen ) );
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
