using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using Laevo.Data.View;
using Laevo.Model;
using Laevo.View.ActivityOverview;
using Laevo.View.Settings;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityBar;
using Laevo.ViewModel.ActivityOverview;
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
		readonly IViewRepository _dataRepository;

		ActivityOverviewWindow _activityOverview;
		ActivityOverviewViewModel _activityOverviewViewModel;

		readonly ActivityBarViewModel _activityBarViewModel = new ActivityBarViewModel();
		readonly View.ActivityBar.ActivityBar _activityBar = new View.ActivityBar.ActivityBar();

		readonly Dispatcher _dispatcher;

		public event Action GuiReset;

		[NotifyProperty( Binding.Properties.UnattendedInterruptions )]
		public int UnattendedInterruptions { get; private set; }

		public MainViewModel( Model.Laevo model, IViewRepository dataRepository )
		{
			_model = model;
			_dataRepository = dataRepository;
			_dispatcher = Dispatcher.CurrentDispatcher;
			_model.LogonScreenExited += () => _dispatcher.Invoke( ResetGui );

			_model.InterruptionAdded += a => { UnattendedInterruptions++; };
			_model.ActivityRemoved += a => UpdateUnattendedInterruptions();

			EnsureActivityOverview();

			_activityBarViewModel.CurrentActivity = _activityOverviewViewModel.CurrentActivityViewModel;
			_activityBarViewModel.HomeActivity = _activityOverviewViewModel.HomeActivity;
			_activityBarViewModel.OpenPlusCurrentActivities = new ObservableCollection<ActivityViewModel>();
			_activityBar.DataContext = _activityBarViewModel;
			ShowActivityBar();
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

		readonly LaevoServiceProvider _serviceProvider = new LaevoServiceProvider();

		[CommandExecute( Commands.Help )]
		public void OpenManual()
		{
			_serviceProvider.Browser.OpenWebsite( "http://whathecode.wordpress.com/laevo/" );
		}

		[CommandExecute( Commands.ShowActivityOverview )]
		public void ShowActivityOverview()
		{
			EnsureActivityOverview();
			_activityOverview.Show();
			_activityOverview.Activate();
		}

		/// <summary>
		/// Opens the activity overview in order to select one of the activities.
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

			if ( _activityOverview.Visibility.EqualsAny( Visibility.Collapsed, Visibility.Hidden ) && !_activityBar.IsActive )
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

		[CommandExecute( Commands.ShowActivityBar )]
		public void ShowActivityBar( bool activate = false )
		{
			if ( _activityOverviewViewModel.CurrentActivityViewModel != null )
			{
				_activityBar.ShowActivityBar( activate );
			}
		}

		[CommandExecute( Commands.OpenCurrentActivityLibrary )]
		public void OpenCurrentActivityLibrary()
		{
			_activityOverviewViewModel.CurrentActivityViewModel.OpenActivityLibrary();
		}

		[CommandExecute( Commands.StopActivity )]
		public void StopActivity()
		{
			if ( _activityOverviewViewModel.CurrentActivityViewModel != null )
			{
				_activityOverviewViewModel.CurrentActivityViewModel.StopActivity();
			}
		}

		[CommandCanExecute( Commands.StopActivity )]
		public bool CanStopActivity()
		{
			var currentActivity = _activityOverviewViewModel.CurrentActivityViewModel;

			return
				currentActivity != null &&
				currentActivity != _activityOverviewViewModel.HomeActivity &&
				currentActivity.IsOpen;
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
				_activityOverviewViewModel = new ActivityOverviewViewModel( _model, _dataRepository )
				{
					TimeLineRenderScale = _model.Settings.TimeLineRenderAtScale,
					EnableAttentionLines = _model.Settings.EnableAttentionLines
				};

				_activityOverviewViewModel.ActivatedActivityEvent += OnActivatedActivityEvent;
				_activityOverviewViewModel.RemovedActivityEvent += OnRemovedActivityEvent;
				_activityOverviewViewModel.NoCurrentActiveActivityEvent += OnNoCurrentActiveActivityEvent;
				_activityOverviewViewModel.OpenedActivityEvent += OnOpenedActivityEvent;
				_activityOverviewViewModel.StoppedActivityEvent += OnStoppedActivityEvent;
			}
			_activityOverview = new ActivityOverviewWindow
			{
				DataContext = _activityOverviewViewModel
			};
			_activityOverview.Activated += ( sender, args ) => _activityOverviewViewModel.OnOverviewActivated();
		}

		void OnActivatedActivityEvent( ActivityViewModel oldActivity, ActivityViewModel newActivity )
		{
			UpdateUnattendedInterruptions();

			// Links CurrentActivity to Current Activity notify property in ActivityBarViewModel.
			_activityBarViewModel.CurrentActivity = _activityOverviewViewModel.CurrentActivityViewModel;

			if ( oldActivity != newActivity )
			{
				if ( oldActivity != null && !oldActivity.IsOpen )
				{
					_activityBarViewModel.OpenPlusCurrentActivities.Remove( oldActivity );
				}

				// Checks if new activity is in the list, if no adds it on front, if yes changes its positoin to first- behavior to simulate alt+tab switching.
				int newActivityIndex = _activityBarViewModel.OpenPlusCurrentActivities.IndexOf( newActivity );
				if ( newActivity != null && newActivityIndex == -1 && newActivity != _activityOverviewViewModel.HomeActivity )
				{
					_activityBarViewModel.OpenPlusCurrentActivities.Insert( 0, newActivity );
				}
				else if ( newActivityIndex != -1 )
				{
					_activityBarViewModel.OpenPlusCurrentActivities.Move( newActivityIndex, 0 );
				}
			}

			HideActivityOverview();

			// TODO: Is there a better way to check whether the name has been set already? Perhaps it's also not desirable to activate the activity bar each time as long as the name isn't changed?
			ShowActivityBar( newActivity.Label == Model.Laevo.DefaultActivityName );
		}

		void OnRemovedActivityEvent( ActivityViewModel removed )
		{
			_activityBarViewModel.OpenPlusCurrentActivities.Remove( removed );
		}

		void OnNoCurrentActiveActivityEvent()
		{
			// Open time line in order to select a new activity to continue work on.
			SelectActivity( a => a.ActivateActivity( a.IsOpen ) );
		}

		void OnOpenedActivityEvent( ActivityViewModel opened )
		{
			if ( !_activityBarViewModel.OpenPlusCurrentActivities.Contains( opened ) )
			{
				_activityBarViewModel.OpenPlusCurrentActivities.Add( opened );
			}
		}

		void OnStoppedActivityEvent( ActivityViewModel stopped )
		{
			_activityBarViewModel.OpenPlusCurrentActivities.Remove( stopped );
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