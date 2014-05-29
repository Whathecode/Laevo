using System;
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

		readonly ActivityBarViewModel _activityBarViewModel;
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

			_activityBarViewModel = new ActivityBarViewModel( _activityOverviewViewModel );
			_activityBar.DataContext = _activityBarViewModel;
			ShowActivityBar( true );
		}


		/// <summary>
		///   HACK: This functionality is provided since this is still a prototype and sometimes the GUI seems to hang.
		///			This could be due to a possible WPF bug:
		///			https://connect.microsoft.com/VisualStudio/feedback/details/602232/when-using-cachemode-bitmapcache-upon-waking-up-from-sleep-wpf-rendering-thread-consumes-40-cpu#tabs
		/// </summary>
		void ResetGui()
		{
			if ( _activityOverview != null )
			{
				_activityOverview.Close();
				_activityOverview = null;
				if ( _activityOverviewViewModel.ActivityMode.HasFlag( Mode.Select ) )
				{
					ShowActivityOverview();
				}
			}

			GuiReset();
		}

		void UpdateUnattendedInterruptions()
		{
			UnattendedInterruptions = _model.Activities.Sum( a => a.Interruptions.Count( i => !i.AttendedTo ) );
		}

		public ActivityViewModel GetCurrentActivity()
		{
			return _activityOverviewViewModel.CurrentActivityViewModel;
		}

		[CommandExecute( Commands.Exit )]
		public void Exit()
		{
			// Make sure newly opened windows on the current desk are stored as well.
			_model.DesktopManager.UpdateWindowAssociations();

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

		[CommandCanExecute( Commands.ShowActivityOverview )]
		public bool CanShowActivityOverview()
		{
			return CanSwitchActivityOverview();
		}

		/// <summary>
		///   Opens the activity overview in order to select one of the activities.
		/// </summary>
		/// <param name="selectedActivity">The action to perform on the selected activity.</param>
		public void SelectActivity( Action<ActivityViewModel> selectedActivity )
		{
			_activityOverviewViewModel.ActivityMode |= Mode.Select;
			var awaitOpen = Observable.FromEvent<ActivityViewModel.ActivityEventHandler, ActivityViewModel>(
				h => _activityOverviewViewModel.SelectedActivityEvent += h,
				h => _activityOverviewViewModel.SelectedActivityEvent -= h ).Take( 1 );
			awaitOpen.Subscribe( a =>
			{
				selectedActivity( a );
				_activityOverviewViewModel.ActivityMode &= ~Mode.Select;
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
			return _activityOverviewViewModel.ActivityMode == Mode.Activate && !_activityBar.IsInUse();
		}

		[CommandExecute( Commands.ShowActivityBar )]
		public void ShowActivityBar( bool autoHide )
		{
			if ( _activityOverviewViewModel.CurrentActivityViewModel != null )
			{
				_activityBar.ShowActivityBar( autoHide );
			}
		}

		[CommandExecute( Commands.HideActivityBar )]
		public void HideActivityBar()
		{
			if ( _activityOverviewViewModel.CurrentActivityViewModel != null )
			{
				_activityBar.HideActivityBar();
			}
		}

		[CommandExecute( Commands.NewActivity )]
		public void NewActivity()
		{
			ActivityViewModel activity = _activityOverviewViewModel.CreateNewActivity();
			activity.OpenActivity();
			activity.ActivateActivity();
			_activityOverviewViewModel.ActivityMode &= ~Mode.Select;
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
			_activityBarViewModel.SelectNextActivity();
		}

		[CommandExecute( Commands.ActivateSelectedActivity )]
		public void ActivateSelectedActivity()
		{
			_model.DesktopManager.UpdateWindowAssociations();

			_activityBarViewModel.ActivateSelectedActivity();
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
				_activityOverviewViewModel.SuspendingActivityEvent += OnSuspendingActivityEvent;
				_activityOverviewViewModel.NoCurrentActiveActivityEvent += OnNoCurrentActiveActivityEvent;
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

			HideActivityOverview();

			// TODO: Is there a better way to check whether the name has been set already? Perhaps it's also not desirable to activate the activity bar each time as long as the name isn't changed?
			ShowActivityBar( newActivity.Label != Model.Laevo.DefaultActivityName );
		}

		void OnSuspendingActivityEvent( ActivityViewModel viewmodel )
		{
			ShowActivityBar( false );
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