using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Breakpoints.Common;
using Breakpoints.Managers;
using Laevo.Data;
using Laevo.Data.View;
using Laevo.View.Activity;
using Laevo.View.Common;
using Laevo.View.Notification;
using Laevo.View.User;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview.Binding;
using Laevo.ViewModel.Notification;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;
using Window = ABC.Window;


namespace Laevo.ViewModel.ActivityOverview
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	public class ActivityOverviewViewModel : AbstractViewModel
	{
		readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

		public delegate void ActivitySwitchEventHandler( ActivityViewModel oldActivity, ActivityViewModel newActivity );

		/// <summary>
		///   Event which is triggered when an activity is activated.
		/// </summary>
		public event ActivitySwitchEventHandler ActivatedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is removed.
		/// </summary>
		public event Action<ActivityViewModel> RemovedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is selected.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler SelectedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is opened.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler OpenedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is stopped.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler StoppedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is being suspended.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler SuspendingActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is created.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler ActivityCreatedEvent;

		/// <summary>
		///   Event which is triggered when there currently is no activity open. This can happen when the active activity is closed or removed.
		/// </summary>
		public event Action NoCurrentActiveActivityEvent;

		/// <summary>
		///   Event which is triggered when pop-up window is shown.
		/// </summary>
		public event Action ShowingPopupEvent;

		/// <summary>
		///   Event which is triggered when pop-up window is being closed.
		/// </summary>
		public event Action ClosedPopupEvent;

		readonly Model.Laevo _model;
		readonly IViewRepository _dataRepository;

		/// <summary>
		///   Timer used to update data regularly.
		/// </summary>
		readonly Timer _updateTimer = new Timer( 100 );


		[NotifyProperty( Binding.Properties.TimeLineRenderScale )]
		public float TimeLineRenderScale { get; set; }

		[NotifyProperty( Binding.Properties.EnableAttentionLines )]
		public bool EnableAttentionLines { get; set; }

		/// <summary>
		///   The mode determines which actions are possible within the activity overview.
		/// </summary>
		[NotifyProperty( Binding.Properties.Mode )]
		public Mode ActivityMode { get; set; }

		/// <summary>
		///   The ViewModel of the activity which is currently active.
		///   TODO: Think if property should be moved to MainViewModel.
		/// </summary>
		[NotifyProperty( Binding.Properties.CurrentActivityViewModel )]
		public ActivityViewModel CurrentActivityViewModel { get; private set; }

		[NotifyProperty( Binding.Properties.CurrentTime )]
		public DateTime CurrentTime { get; private set; }

		/// <summary>
		///   Whether or not the currently focused (non-rounded) time lies before the current actual time.
		/// </summary>
		[NotifyProperty( Binding.Properties.IsFocusedTimeBeforeNow )]
		public bool IsFocusedTimeBeforeNow { get; private set; }

		/// <summary>
		///   The time which currently has input focus and can be acted upon. This is rounded to nearest values.
		/// </summary>
		[NotifyProperty( Binding.Properties.FocusedRoundedTime )]
		public DateTime FocusedRoundedTime { get; private set; }

		[NotifyProperty( Binding.Properties.FocusedOffsetPercentage )]
		public double FocusedOffsetPercentage { get; set; }

		[NotifyProperty( Binding.Properties.HomeActivity )]
		public ActivityViewModel HomeActivity { get; private set; }

		/// <summary>
		///   The activity of which the time line is currently visible.
		/// </summary>
		[NotifyProperty( Binding.Properties.VisibleActivity )]
		public ActivityViewModel VisibleActivity { get; private set; }

		[NotifyPropertyChanged( Binding.Properties.HomeActivity )]
		public void OnHomeActivityChanged( ActivityViewModel oldActivity, ActivityViewModel newActivity )
		{
			_dataRepository.Home = newActivity;
		}

		[NotifyProperty( Binding.Properties.Activities )]
		public ReadOnlyObservableCollection<ActivityViewModel> Activities { get; private set; }

		[NotifyProperty( Binding.Properties.Tasks )]
		public ReadOnlyObservableCollection<ActivityViewModel> Tasks { get; private set; }

		[NotifyProperty( Binding.Properties.Path )]
		public List<ActivityViewModel> Path { get; private set; }

		[NotifyProperty( Binding.Properties.Notifications )]
		public ObservableCollection<NotificationViewModel> Notifications { get; private set; }


		public bool IsDisabled
		{
			get { return ActivityMode.HasFlag( Mode.Inactive ); }
		}

		readonly List<NotificationPopup> _notificationPopups = new List<NotificationPopup>();
		readonly NotificationList _notificationList;
		readonly Dictionary<ActivityViewModel, LaevoBreakpointManager> _activityBreakpointManagers = new Dictionary<ActivityViewModel, LaevoBreakpointManager>();

		public ActivityOverviewViewModel( Model.Laevo model, IViewRepository dataRepository )
		{
			_model = model;
			_dataRepository = dataRepository;

			// Set-up all notifications list.
			Notifications = new ObservableCollection<NotificationViewModel>();
			var notificationListImageUri = new Uri( @"/Laevo;component/View/ActivityOverview/Images/Bell.png", UriKind.Relative );
			_notificationList = new NotificationList
			{
				Notifications = Notifications,
				PopupImage = new BitmapImage( notificationListImageUri )
			};

			// Hook interruption breakpoint aggregator.
			_model.Aggregator.InterruptionBreakpointOccured += ( sender, args ) =>
			{
				var notificationViewModel = new NotificationViewModel
				{
					Summary = args.Interruption.Name
				};
				HookNotification( notificationViewModel );
				Dispatcher.CurrentDispatcher.BeginInvoke( new Action( () =>
				{
					Notifications.Add( notificationViewModel );

					// TODO: Decide if show a notification pop-up accordingly to the interruption importance (for now show for all).
					ShowNotification( notificationViewModel );
				} ) );


				// TODO: Properly assign a notification to a specific activity if any (for now all go to the home activity).
				HomeActivity.Notifications.Add( notificationViewModel );
				HomeActivity.Activity.AddInterruption( args.Interruption );
			};

			var overviewLaevoBreakpointManager = new LaevoBreakpointManager( Guid.NewGuid() );
			_model.Aggregator.AddManager( overviewLaevoBreakpointManager );
			// Medium events.
			overviewLaevoBreakpointManager.RegisterBreakpoint( "ActivatedActivityEvent", this, BreakpointType.Medium );
			overviewLaevoBreakpointManager.RegisterBreakpoint( "ClosedPopupEvent", this, BreakpointType.Medium );
			overviewLaevoBreakpointManager.RegisterBreakpoint( "ActivityCreatedEvent", this, BreakpointType.Medium );
			// Coarse events.
			overviewLaevoBreakpointManager.RegisterBreakpoint( "RemovedActivityEvent", this, BreakpointType.Coarse );
			overviewLaevoBreakpointManager.RegisterBreakpoint( "SelectedActivityEvent", this, BreakpointType.Coarse );
			overviewLaevoBreakpointManager.RegisterBreakpoint( "StoppedActivityEvent", this, BreakpointType.Coarse );
			overviewLaevoBreakpointManager.RegisterBreakpoint( "SuspendingActivityEvent", this, BreakpointType.Coarse );
			overviewLaevoBreakpointManager.RegisterBreakpoint( "NoCurrentActiveActivityEvent", this, BreakpointType.Coarse );

			// Set up home activity.
			if ( _dataRepository.Home != null )
			{
				HomeActivity = _dataRepository.Home;
			}
			else
			{
				HomeActivity = new ActivityViewModel( _model.HomeActivity, _model.WorkspaceManager, _dataRepository, _model.WorkspaceManager.StartupWorkspace )
				{
					Icon = ActivityViewModel.PresetIcons.First( b => b.UriSource.AbsolutePath.Contains( "home.png" ) ),
					IsEditable = false
				};
			}
			HookActivityToOverview( HomeActivity );
			HomeActivity.ActivateActivity( false );

			ActivityMode |= Mode.Activate;

			// Load personal activities.
			UnloadActivities();
			_model.ChangeToPersonalTimeLine();
			_dataRepository.LoadPersonalActivities();
			Activities = _dataRepository.Activities;
			Tasks = _dataRepository.Tasks;
			Activities.Union( Tasks ).ForEach( HookActivityToOverview );

			// Listen for new interruption tasks being added.
			// TODO: This probably needs to be removed as it is a bit messy. A better communication from the model to the viewmodel needs to be devised.
			_model.InterruptionAdded += task =>
			{
				var taskViewModel = new ActivityViewModel( task, _model.WorkspaceManager, _dataRepository )
				{
					// TODO: This is hardcoded for this release where only gmail is supported, but allow the plugin to choose the layout.
					Color = ActivityViewModel.PresetColors[ 5 ],
					Icon = ActivityViewModel.PresetIcons.First( b => b.UriSource.AbsolutePath.Contains( "mail_read.png" ) )
				};
				AddActivity( taskViewModel, HomeActivity );
			};

			// Listen for invited activities being added.
			// TODO: This probably needs to be removed as it is a bit messy. A better communication from the model to the viewmodel needs to be devised.
			_model.InvitedToActivity += activity =>
			{
				var activityViewModel = new ActivityViewModel( activity, _model.WorkspaceManager, _dataRepository );
				// TODO: Allow changing name/icon/color and only maintain subactivities?
				AddActivity( activityViewModel, HomeActivity );
			};

			// Hook up timer.
			// TODO: Sometimes throws exception during exit: "Task was canceled." Needs more testing, maybe try, catch should be added to suppress exception.
			_updateTimer.Elapsed += ( s, a ) => _dispatcher.Invoke( () => UpdateData( a.SignalTime ) );

			_updateTimer.Start();
		}


		void UnloadActivities()
		{
			// Clear previously loaded activities.
			if ( Activities != null )
			{
				Activities.ForEach( UnHookActivityFromOverview );
			}
			if ( Tasks != null )
			{
				Tasks.ForEach( UnHookActivityFromOverview );
			}

			// Unload path.
			if ( Path != null )
			{
				Path.Skip( 1 ).ForEach( UnHookActivityFromOverview ); // Skip home activity.
				Path = null;
			}
		}

		/// <summary>
		///   Initialize the activities and tasks to work with this overview by hooking up activity view models from previous sessions.
		/// </summary>
		public void LoadActivities( ActivityViewModel parentActivity )
		{
			UnloadActivities();

			VisibleActivity = parentActivity ?? HomeActivity; // Load home activity first time.
			_model.ChangeVisibleTimeLine( VisibleActivity.Activity );

			// Load new activities.
			_dataRepository.LoadActivities( VisibleActivity.Activity );
			Activities = _dataRepository.Activities;
			Tasks = _dataRepository.Tasks;
			Activities.Union( Tasks ).ForEach( HookActivityToOverview );

			// Set path.
			Path = _dataRepository.GetPath( VisibleActivity );
			Path.ForEach( HookActivityToOverview );
		}

		/// <summary>
		///   Create a new activity.
		/// </summary>
		public ActivityViewModel CreateNewActivity()
		{
			ActivityViewModel parent = HomeActivity;
			Model.Activity activity = _model.CreateNewActivity( parent.Activity );
			var newActivity = new ActivityViewModel( activity, _model.WorkspaceManager, _dataRepository )
			{
				ShowActiveTimeSpans = _model.Settings.EnableAttentionLines,
				IsUnnamed = true
			};

			ActivityCreatedEvent( newActivity );
			AddActivity( newActivity );

			return newActivity;
		}

		public void MoveActivity( ActivityViewModel activity, ActivityViewModel toParent )
		{
			if ( toParent == activity )
			{
				return;
			}

			_model.MoveActivity( activity.Activity, toParent.Activity );
			_dataRepository.MoveActivity( activity, toParent );
		}

		/// <summary>
		///   Adds an activity to the specified parent.
		///   When the parent is not specified, the activity is added to the current open time line in hierarchy view, or home in personal view.
		/// </summary>
		void AddActivity( ActivityViewModel activity, ActivityViewModel parent = null )
		{
			if ( parent == null )
			{
				parent = HomeActivity;
			}

			_dataRepository.AddActivity( activity, parent );
			HookActivityToOverview( activity );
		}

		[CommandExecute( Commands.NewTask )]
		public void NewTask()
		{
			ActivityViewModel parent = HomeActivity;
			Model.Activity task = _model.CreateNewTask( parent.Activity );
			var newTask = new ActivityViewModel( task, _model.WorkspaceManager, _dataRepository )
			{
				ShowActiveTimeSpans = _model.Settings.EnableAttentionLines,
				IsUnnamed = true
			};
			AddActivity( newTask );

			ActivityCreatedEvent( newTask );

			_model.Aggregator.RegisterInterruption( new Model.Laevo.TestInterruption( "test name" ), BreakpointType.Fine );
		}

		[CommandExecute( Commands.NewActivity )]
		public void NewActivity()
		{
			ActivityViewModel activity = CreateNewActivity();
			activity.OpenActivity();
			PositionActivityAtCurrentOffset( activity );

			activity.EditActivity();
		}

		[CommandExecute( Commands.PlanActivity )]
		public void PlanActivity()
		{
			ActivityViewModel activity = CreateNewActivity();
			activity.Plan( FocusedRoundedTime );
			PositionActivityAtCurrentOffset( activity );

			activity.EditActivity();
		}

		[CommandCanExecute( Commands.PlanActivity )]
		public bool CanPlanActivity()
		{
			return FocusedRoundedTime > DateTime.Now;
		}

		[CommandExecute( Commands.ShowNotifications )]
		public void ShowNotifications()
		{
			_notificationPopups.ForEach( notificationPopup => { notificationPopup.Close(); } );
			Dispatcher.CurrentDispatcher.BeginInvoke( new Action( () => _notificationPopups.Clear() ) );
			_notificationList.Show();
		}

		[CommandCanExecute( Commands.ShowNotifications )]
		public bool CanShowNotifications()
		{
			return Notifications.Count > 0;
		}

		/// <summary>
		///   Remove a task or activity.
		/// </summary>
		/// <param name = "activity">The task or activity to remove.</param>
		public void Remove( ActivityViewModel activity )
		{
			if ( CurrentActivityViewModel == activity )
			{
				CurrentActivityViewModel = null;
				NoCurrentActiveActivityEvent();
			}

			_model.Remove( activity.Activity );
			_dataRepository.RemoveActivity( activity );

			UnHookActivityFromOverview( activity );

			RemovedActivityEvent( activity );
		}

		public void SwapTaskOrder( ActivityViewModel task1, ActivityViewModel task2 )
		{
			_dataRepository.SwapTaskOrder( task1, task2 );
			_model.SwapTaskOrder( task1.Activity, task2.Activity );
		}

		public void ShowNotification( NotificationViewModel notificationViewModel )
		{
			var notificationPopup = new NotificationPopup
			{
				DataContext = notificationViewModel
			};
			_notificationPopups.Add( notificationPopup );
			HookNotificationPopup( notificationViewModel, notificationPopup );
			notificationPopup.Show();
		}

		void HookNotificationPopup( NotificationViewModel notificationViewModel, NotificationPopup notificationPopup )
		{
			notificationViewModel.Index = _notificationPopups.Count;
			notificationViewModel.PopupHiding += ( o, eventArgs ) =>
			{
				_notificationPopups.Remove( notificationPopup );
				notificationPopup.Close();
			};
		}

		public void HookNotification( NotificationViewModel notificationViewModel )
		{
			notificationViewModel.NewActivityCreating += ( o, eventArgs ) =>
			{
				var senderNotification = (NotificationViewModel)o;

				var activity = CreateNewActivity();
				activity.Label = senderNotification.Summary;
				activity.Activity.Name = senderNotification.Summary;

				activity.OpenActivity();
				PositionActivityAtCurrentOffset( activity );

				activity.EditActivity();
				activity.ActivateActivity();
			};
			notificationViewModel.NewTaskCreating += ( o, eventArgs ) =>
			{
				var senderNotification = (NotificationViewModel)o;

				var parent = HomeActivity;
				var task = _model.CreateNewTask( parent.Activity );
				task.Name = senderNotification.Summary;

				var newTask = new ActivityViewModel( task, _model.WorkspaceManager, _dataRepository )
				{
					ShowActiveTimeSpans = _model.Settings.EnableAttentionLines,
					Label = senderNotification.Summary
				};
				AddActivity( newTask );
				newTask.EditActivity();
			};

			notificationViewModel.NotificationRemoving += ( o, args ) =>
			{
				var senderNotification = (NotificationViewModel)o;

				// Remove notification from the overview.
				Notifications.Remove( senderNotification );

				// Remove notification from all the activities.
				Activities.ForEach( activity => activity.Notifications.Remove( senderNotification ) );
				// And from Home activity.
				HomeActivity.Notifications.Remove( senderNotification );
			};
		}

		void UnHookActivityFromOverview( ActivityViewModel activity )
		{
			activity.ActivatingActivityEvent -= OnActivityActivating;
			activity.ActivatedActivityEvent -= OnActivityActivated;
			activity.SelectedActivityEvent -= OnActivitySelected;
			activity.ActivityStoppedEvent -= OnActivityStopped;
			activity.SuspendingActivityEvent -= OnSuspendingActivity;
			activity.SuspendedActivityEvent -= OnSuspendedActivity;
			activity.ToDoChangedEvent -= OnToDoChanged;
			activity.ClaimedOwnershipEvent -= OnClaimedOwnership;
			activity.DroppedOwnershipEvent -= OnDroppedOwnership;

			UnhookFromBreakpointManager( activity );
		}

		void HookActivityToOverview( ActivityViewModel activity )
		{
			activity.SetOverviewManager( this );

			// Make sure the events are never hooked twice.
			UnHookActivityFromOverview( activity );

			activity.ActivatingActivityEvent += OnActivityActivating;
			activity.ActivatedActivityEvent += OnActivityActivated;
			activity.SelectedActivityEvent += OnActivitySelected;
			activity.ActivityStoppedEvent += OnActivityStopped;
			activity.SuspendingActivityEvent += OnSuspendingActivity;
			activity.SuspendedActivityEvent += OnSuspendedActivity;
			activity.ToDoChangedEvent += OnToDoChanged;
			activity.ClaimedOwnershipEvent += OnClaimedOwnership;
			activity.DroppedOwnershipEvent += OnDroppedOwnership;

			HookToBreakpointManager( activity );
		}

		void UnhookFromBreakpointManager( ActivityViewModel activityViewModel )
		{
			LaevoBreakpointManager activityLaevoBreakpointManager;
			_activityBreakpointManagers.TryGetValue( activityViewModel, out activityLaevoBreakpointManager );
			if ( activityLaevoBreakpointManager == null )
				return;

			activityLaevoBreakpointManager.UnregisterBreakpoint( "DeactivatedEvent", activityViewModel.Activity, BreakpointType.Medium );
			activityLaevoBreakpointManager.UnregisterBreakpoint( "ToDoChangedEvent", activityViewModel.Activity, BreakpointType.Coarse );
			activityLaevoBreakpointManager.UnregisterBreakpoint( "AccessAddedEvent", activityViewModel.Activity, BreakpointType.Coarse );
			activityLaevoBreakpointManager.UnregisterBreakpoint( "AccessRemovedEvent", activityViewModel.Activity, BreakpointType.Coarse );
			activityLaevoBreakpointManager.UnregisterBreakpoint( "OwnershipAddedEvent", activityViewModel.Activity, BreakpointType.Coarse );
			activityLaevoBreakpointManager.UnregisterBreakpoint( "OpenedEvent", activityViewModel.Activity, BreakpointType.Coarse );

			_activityBreakpointManagers.Remove( activityViewModel );
		}

		void HookToBreakpointManager( ActivityViewModel activityViewModel )
		{
			var activityLaevoBreakpointManager = new LaevoBreakpointManager( Guid.NewGuid() );
			_activityBreakpointManagers.Add( activityViewModel, activityLaevoBreakpointManager );
			_model.Aggregator.AddManager( activityLaevoBreakpointManager );

			activityLaevoBreakpointManager.RegisterBreakpoint( "DeactivatedEvent", activityViewModel.Activity, BreakpointType.Medium );
			activityLaevoBreakpointManager.RegisterBreakpoint( "ToDoChangedEvent", activityViewModel.Activity, BreakpointType.Coarse );
			activityLaevoBreakpointManager.RegisterBreakpoint( "AccessAddedEvent", activityViewModel.Activity, BreakpointType.Coarse );
			activityLaevoBreakpointManager.RegisterBreakpoint( "AccessRemovedEvent", activityViewModel.Activity, BreakpointType.Coarse );
			activityLaevoBreakpointManager.RegisterBreakpoint( "OwnershipAddedEvent", activityViewModel.Activity, BreakpointType.Coarse );
			activityLaevoBreakpointManager.RegisterBreakpoint( "OpenedEvent", activityViewModel.Activity, BreakpointType.Coarse );
		}

		void OnActivityActivating( ActivityViewModel viewModel )
		{
			// Indicate the previously active activity is no longer active (visible).
			if ( CurrentActivityViewModel != null && viewModel != CurrentActivityViewModel )
			{
				CurrentActivityViewModel.Deactivated();
			}
		}

		void OnActivityActivated( ActivityViewModel viewModel )
		{
			ActivityViewModel oldActivity = CurrentActivityViewModel;
			CurrentActivityViewModel = viewModel;
			ActivatedActivityEvent( oldActivity, CurrentActivityViewModel );
		}

		void OnActivityStopped( ActivityViewModel viewModel )
		{
			DeactivateWithoutSwitch( viewModel );
			StoppedActivityEvent( viewModel );
		}

		void OnActivitySelected( ActivityViewModel viewModel )
		{
			SelectedActivityEvent( viewModel );
		}

		void OnSuspendingActivity( ActivityViewModel viewModel )
		{
			ActivityMode |= Mode.Suspending;
			SuspendingActivityEvent( viewModel );
		}

		void OnSuspendedActivity( ActivityViewModel viewModel )
		{
			ActivityMode &= ~Mode.Suspending;
			DeactivateWithoutSwitch( viewModel );
		}

		/// <summary>
		///   Needs to be called when an activity is deactived without immediately switching to another activity, meaning no activity is active at all.
		/// </summary>
		void DeactivateWithoutSwitch( ActivityViewModel viewModel )
		{
			if ( viewModel == CurrentActivityViewModel )
			{
				CurrentActivityViewModel = null;
				NoCurrentActiveActivityEvent();
			}
		}

		void OnToDoChanged( ActivityViewModel viewModel )
		{
			_dataRepository.UpdateActivity( viewModel );
		}

		void OnClaimedOwnership( ActivityViewModel viewModel )
		{
			_dataRepository.UpdateActivity( viewModel );
		}

		void OnDroppedOwnership( ActivityViewModel viewModel )
		{
			_dataRepository.UpdateActivity( viewModel );
		}

		[CommandExecute( Commands.OpenHome )]
		public void OpenHome()
		{
			HomeActivity.SelectActivity();
		}

		[CommandExecute( Commands.OpenUserProfile )]
		public void OpenUserProfile()
		{
			var profile = new UserProfilePopup
			{
				DataContext = _dataRepository.User
			};
			profile.Closed += ( s, a ) => _dataRepository.User.Persist();

			ShowPopup( profile );
		}

		[CommandExecute( Commands.OpenTimeLineSharing )]
		public void OpenTimeLineSharing( ActivityViewModel activityViewModel = null )
		{
			var share = new SharePopup
			{
				DataContext = new ShareViewModel( _model, activityViewModel ?? CurrentActivityViewModel, _dataRepository )
			};
			share.Closed += ( s, a ) => VisibleActivity.Persist();

			ShowPopup( share );
		}

		// ReSharper disable UnusedMember.Local
		// ReSharper disable UnusedParameter.Local
		[NotifyPropertyChanged( Binding.Properties.EnableAttentionLines )]
		void OnEnableAttentionLinesChanged( bool oldIsEnabled, bool newIsEnabled )
		{
			foreach ( var activity in Activities.Concat( Tasks ).Distinct() )
			{
				activity.ShowActiveTimeSpans = newIsEnabled;
			}
		}

		// ReSharper restore UnusedMember.Local
		// ReSharper restore UnusedParameter.Local

		void UpdateData( DateTime updateTime )
		{
			CurrentTime = updateTime;

			// Update model.
			_model.Update( CurrentTime );

			// Update required view models.
			if ( Activities != null )
			{
				Activities.ForEach( a => a.Update( CurrentTime ) );
			}
			if ( Tasks != null )
			{
				Tasks.ForEach( t => t.Update( CurrentTime ) );
			}
		}

		public void ShowPopup( LaevoPopup popup )
		{
			popup.Closed += ( sender, args ) => { ClosedPopupEvent(); };
			ShowingPopupEvent();
			ActivityMode |= Mode.Inactive;
			popup.ShowDialog();
			ActivityMode &= ~Mode.Inactive;
		}

		public void CutWindow()
		{
			_model.WindowClipboard.CutWindow( Window.GetForegroundWindow() );
		}

		public void PasteWindows()
		{
			_model.WindowClipboard.PasteWindows();
		}

		public void ActivityDropped( ActivityViewModel activity )
		{
			// Based on where the activity is dropped, open, or plan it.
			bool openEdit = false;
			if ( IsFocusedTimeBeforeNow )
			{
				if ( activity.GetFutureWorkIntervals().Any() )
				{
					activity.RemovePlanning();
				}

				activity.OpenActivity();
			}
			else
			{
				activity.Plan( FocusedRoundedTime );
				openEdit = true;
			}

			PositionActivityAtCurrentOffset( activity );

			// In case the activity was planned, open edit dialog.
			// Opening the dialog takes some time, so the PositionActivityAtCurrentOffset() is visually noticeable if called before EditActivity(). Hence the boolean.
			if ( openEdit )
			{
				activity.EditActivity( true );
			}
		}

		void PositionActivityAtCurrentOffset( ActivityViewModel activity )
		{
			var workInterval = activity.WorkIntervals.Last();
			var offsetRange = new Interval<double>( workInterval.HeightPercentage, 1 );
			var percentageInterval = new Interval<double>( 0, 1 );
			workInterval.OffsetPercentage = offsetRange.Map( FocusedOffsetPercentage, percentageInterval ).Clamp( 0, 1 );
		}

		public void FocusedTimeChanged( DateTime focusedTime )
		{
			IsFocusedTimeBeforeNow = focusedTime <= DateTime.Now;

			// Set rounded focus time.
			DateTime focused = Model.Laevo.GetNearestTime( focusedTime );
			if ( focused < DateTime.Now )
			{
				focused = focused.SafeAdd( TimeSpan.FromMinutes( Model.Laevo.SnapToMinutes ) );
			}
			FocusedRoundedTime = focused;
		}

		public void Exit()
		{
			if ( CurrentActivityViewModel != null )
			{
				// Ensure that operations which are performed at deactivation are still executed.
				CurrentActivityViewModel.Deactivated();
			}
		}

		public override void Persist()
		{
			try
			{
				_dataRepository.SaveChanges();
			}
			catch ( PersistenceException pe )
			{
				View.MessageBox.Show( pe.Message, "Saving view data failed", MessageBoxButton.OK );
			}
		}

		protected override void FreeUnmanagedResources()
		{
			// Make sure timer does not dispatch another task.
			_updateTimer.AutoReset = false;
			_updateTimer.Stop();
			Activities.Concat( Tasks ).Distinct().ForEach( a => a.Dispose() );
		}
	}
}