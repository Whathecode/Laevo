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
using Laevo.ViewModel.Main.Unresponsive;
using Laevo.ViewModel.Settings;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;
using Application = System.Windows.Application;
using Commands = Laevo.ViewModel.Main.Binding.Commands;
using UnresponsiveWindowPopup = Laevo.View.Main.Unresponsive.UnresponsiveWindowPopup;


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

		readonly UnresponsiveWindowPopup _unresponsivePopup = new UnresponsiveWindowPopup();
		bool _unresponsiveEventThrown;


		public MainViewModel( Model.Laevo model, IViewRepository dataRepository )
		{
			model.WindowClipboard.UnresponsiveWindowDetected += ( windows, desktop ) =>
			{
				var unresponsiveViewModel = new UnresponsiveViewModel( windows );
				unresponsiveViewModel.UnresponsiveHandled += () => _unresponsivePopup.Hide();
				_unresponsivePopup.DataContext = unresponsiveViewModel;
				ShowActivityOverview();
				_unresponsiveEventThrown = true;

				_activityOverviewViewModel.ShowPopup( _unresponsivePopup );
			};

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
			UnattendedInterruptions = _model.GetUnattendedInterruptions().Sum( i => i.Value.Count );
		}

		public ActivityViewModel GetCurrentActivity()
		{
			return _activityOverviewViewModel.CurrentActivityViewModel;
		}

		[CommandExecute( Commands.Exit )]
		public void Exit()
		{
			_activityOverviewViewModel.Exit();
			_model.Exit();
			Persist();

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
			_activityBar.Hide();
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
			return _activityOverviewViewModel.ActivityMode.EqualsAny( Mode.Activate, Mode.Hierarchies ) && !_activityBar.IsInUse()
			       && !_activityOverviewViewModel.IsDisabled;
		}

		[CommandExecute( Commands.ShowActivityBar )]
		public void ShowActivityBar( bool autoHide )
		{
			if ( _activityOverviewViewModel.CurrentActivityViewModel != null && !_activityOverviewViewModel.IsDisabled )
			{
				_activityBar.ShowActivityBar( autoHide );
			}
		}

		[CommandExecute( Commands.HideActivityBar )]
		public void HideActivityBar()
		{
			if ( _activityOverviewViewModel.CurrentActivityViewModel != null && !_activityOverviewViewModel.IsDisabled )
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

		[CommandCanExecute( Commands.ShowActivityBar ),
		 CommandCanExecute( Commands.HideActivityBar ),
		 CommandCanExecute( Commands.NewActivity ),
		 CommandCanExecute( Commands.CutWindow ),
		 CommandCanExecute( Commands.PasteWindows ),
		 CommandCanExecute( Commands.SwitchActivity ),
		 CommandCanExecute( Commands.ActivateSelectedActivity )]
		public bool CanExecuteShortcut()
		{
			return !_activityOverviewViewModel.IsDisabled;
		}

		[CommandExecute( Commands.ActivateSelectedActivity )]
		public void ActivateSelectedActivity()
		{
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
				_activityOverviewViewModel.ShowingPopupEvent += () => _activityBar.Hide();
			}
			_activityOverview = new ActivityOverviewWindow
			{
				DataContext = _activityOverviewViewModel
			};
			_activityOverview.Closed += ( s, a ) => ResetGui();
		}

		void OnActivatedActivityEvent( ActivityViewModel oldActivity, ActivityViewModel newActivity )
		{
			UpdateUnattendedInterruptions();

			// When pop-up window with unresponsive windows is shown we do not want to hide the overview.
			if ( !_unresponsiveEventThrown )
			{
				HideActivityOverview();
			}
			else
			{
				_unresponsiveEventThrown = false;
			}

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