using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using Laevo.Data;
using Laevo.Data.View;
using Laevo.View.Common;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview.Binding;
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
		///   Event which is triggered when pop-up window is opened.
		/// </summary>
		public event EventHandler OpenedPopupEvent;

		/// <summary>
		///   Event which is triggered when there currently is no activity open. This can happen when the active activity is closed or removed.
		/// </summary>
		public event Action NoCurrentActiveActivityEvent;

		/// <summary>
		///   Event which is triggered when pop-up window is shown;
		/// </summary>
		public event Action ShowingPopupEvent;

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
		///   The ViewModel of the activity which is currently open.
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

		[NotifyPropertyChanged( Binding.Properties.HomeActivity )]
		public void OnHomeActivityChanged( ActivityViewModel oldActivity, ActivityViewModel newActivity )
		{
			_dataRepository.Home = newActivity;
		}

		public ObservableCollection<ActivityViewModel> Activities
		{
			get { return _dataRepository.Activities; }
		}

		public ObservableCollection<ActivityViewModel> Tasks
		{
			get { return _dataRepository.Tasks; }
		}

		public bool IsActive
		{
			get { return !ActivityMode.HasFlag( Mode.Inactive ); }
		}


		public ActivityOverviewViewModel( Model.Laevo model, IViewRepository dataRepository )
		{
			_model = model;
			_dataRepository = dataRepository;

			// Set up home activity.
			if ( _dataRepository.Home != null )
			{
				HomeActivity = _dataRepository.Home;
			}
			else
			{
				HomeActivity = new ActivityViewModel( _model.HomeActivity, _model.WorkspaceManager, _model.WorkspaceManager.StartupWorkspace )
				{
					Icon = ActivityViewModel.PresetIcons.First( b => b.UriSource.AbsolutePath.Contains( "home.png" ) ),
					IsEditable = false
				};
			}
			HookActivityToOverview( HomeActivity );
			HomeActivity.ActivateActivity( false );

			// Initialize the activities and tasks to work with this overview by hooking up activity view models from previous sessions.
			Activities.Concat( Tasks ).Distinct().ForEach( HookActivityToOverview );

			// Listen for new interruption tasks being added.
			// TODO: This probably needs to be removed as it is a bit messy. A better communication from the model to the viewmodel needs to be devised.
			_model.InterruptionAdded += task =>
			{
				var taskViewModel = new ActivityViewModel( task, _model.WorkspaceManager )
				{
					// TODO: This is hardcoded for this release where only gmail is supported, but allow the plugin to choose the layout.
					Color = ActivityViewModel.PresetColors[ 5 ],
					Icon = ActivityViewModel.PresetIcons.First( b => b.UriSource.AbsolutePath.Contains( "mail_read.png" ) )
				};
				AddTask( taskViewModel );
			};

			// Hook up timer.
			_updateTimer.Elapsed += ( s, a ) => _dispatcher.Invoke( () => UpdateData( a.SignalTime ) );
			_updateTimer.Start();
		}


		/// <summary>
		///   Create a new activity.
		/// </summary>
		public ActivityViewModel CreateNewActivity()
		{
			var newActivity = new ActivityViewModel( _model.CreateNewActivity(), _model.WorkspaceManager )
			{
				ShowActiveTimeSpans = _model.Settings.EnableAttentionLines,
				IsUnnamed = true
			};
			{
				Activities.Add( newActivity );
			}

			HookActivityToOverview( newActivity );

			return newActivity;
		}

		[CommandExecute( Commands.NewTask )]
		public void NewTask()
		{
			var newTask = new ActivityViewModel( _model.CreateNewTask(), _model.WorkspaceManager )
			{
				ShowActiveTimeSpans = _model.Settings.EnableAttentionLines,
				IsUnnamed = true
			};
			AddTask( newTask );
		}

		void AddTask( ActivityViewModel task )
		{
			if ( !task.Activity.IsToDo )
			{
				throw new ArgumentException( "The passed activity is not a to-do item.", "task" );
			}

			lock ( Tasks )
			{
				Tasks.Insert( 0, task );
			}
			HookActivityToOverview( task );
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

			lock ( Activities )
			{
				Activities.Remove( activity );
			}
			lock ( Tasks )
			{
				Tasks.Remove( activity );
			}

			UnHookActivityFromOverview( activity );

			RemovedActivityEvent( activity );
		}

		public void SwapTaskOrder( ActivityViewModel task1, ActivityViewModel task2 )
		{
			// Update viewmodel.
			int draggedIndex = Tasks.IndexOf( task1 );
			int currentIndex = Tasks.IndexOf( task2 );
			var reordered = Tasks
				.Select( ( t, i ) => i == draggedIndex ? currentIndex : i == currentIndex ? draggedIndex : i )
				.Select( toAdd => Tasks[ toAdd ] )
				.ToArray();
			Tasks.Clear();
			reordered.ForEach( Tasks.Add );

			// Update order in model as well.
			_model.SwapTaskOrder( task1.Activity, task2.Activity );
		}

		void UnHookActivityFromOverview( ActivityViewModel activity )
		{
			activity.ActivatingActivityEvent -= OnActivityActivating;
			activity.ActivatedActivityEvent -= OnActivityActivated;
			activity.SelectedActivityEvent -= OnActivitySelected;
			activity.ActivityEditStartedEvent -= OnPopupShowing;
			activity.ActivityEditFinishedEvent -= OnPopupClosed;
			activity.ActivityStoppedEvent -= OnActivityStopped;
			activity.SuspendingActivityEvent -= OnSuspendingActivity;
			activity.SuspendedActivityEvent -= OnSuspendedActivity;
			activity.ToDoChangedEvent -= OnToDoChanged;
		}

		void HookActivityToOverview( ActivityViewModel activity )
		{
			activity.SetOverviewManager( this );

			// Make sure the events are never hooked twice.
			UnHookActivityFromOverview( activity );

			activity.ActivatingActivityEvent += OnActivityActivating;
			activity.ActivatedActivityEvent += OnActivityActivated;
			activity.SelectedActivityEvent += OnActivitySelected;
			activity.ActivityEditStartedEvent += OnPopupShowing;
			activity.ActivityEditFinishedEvent += OnPopupClosed;
			activity.ActivityStoppedEvent += OnActivityStopped;
			activity.SuspendingActivityEvent += OnSuspendingActivity;
			activity.SuspendedActivityEvent += OnSuspendedActivity;
			activity.ToDoChangedEvent += OnToDoChanged;
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

		public void OnPopupShowing( ActivityViewModel viewModel = null )
		{
			ActivityMode |= Mode.Inactive;
			OpenedPopupEvent( this, new EventArgs() );
		}

		public void OnPopupClosed( ActivityViewModel viewModel = null )
		{
			ActivityMode &= ~Mode.Inactive;
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
			bool isTurnedIntoToDo = viewModel.Activity.IsToDo;

			if ( isTurnedIntoToDo )
			{
				AddTask( viewModel );
			}
			else
			{
				Tasks.Remove( viewModel );
			}
		}

		[CommandExecute( Commands.OpenHome )]
		public void OpenHome()
		{
			HomeActivity.SelectActivity();
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
			lock ( Activities )
			{
				if ( Activities != null )
				{
					Activities.ForEach( a => a.Update( CurrentTime ) );
				}
			}
			lock ( Tasks )
			{
				if ( Tasks != null )
				{
					Tasks.ForEach( t => t.Update( CurrentTime ) );
				}
			}
		}

		public void ShowPopup( LaevoPopup popup )
		{
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
			if ( activity.IsToDo )
			{
				// Convert to activity.
				Tasks.Remove( activity );
				if ( !Activities.Contains( activity ) ) // Activity can already have a presentation on the time line when it was converted to a to do item before.
				{
					Activities.Add( activity );
				}
			}

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
			_updateTimer.Stop();
			Activities.Concat( Tasks ).Distinct().ForEach( a => a.Dispose() );
		}
	}
}