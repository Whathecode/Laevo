using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Timers;
using Laevo.Model.AttentionShifts;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview.Binding;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;
using Whathecode.System.Windows.Interop;
using Whathecode.VirtualDesktopManagerAPI;
using Whathecode.VirtualDesktopManagerAPI.Settings;


namespace Laevo.ViewModel.ActivityOverview
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class ActivityOverviewViewModel : AbstractViewModel
	{
		static readonly string ActivitiesFile = Path.Combine( Model.Laevo.ProgramDataFolder, "ActivityRepresentations.xml" );
		static readonly string TasksFile = Path.Combine( Model.Laevo.ProgramDataFolder, "TaskRepresentations.xml" );


		public delegate void ActivitySwitchEventHandler( ActivityViewModel oldActivity, ActivityViewModel newActivity );


		/// <summary>
		///   Event which is triggered when an activity is opened.
		/// </summary>
		public event ActivitySwitchEventHandler ActivatedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is selected.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler SelectedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is closed.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler ClosedActivityEvent;

		readonly Model.Laevo _model;
		readonly DesktopManager _desktopManager;

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
		/// </summary>
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

		[NotifyProperty( Binding.Properties.Activities )]
		public ObservableCollection<ActivityViewModel> Activities { get; private set; }

		[NotifyProperty( Binding.Properties.Tasks )]
		public ObservableCollection<ActivityViewModel> Tasks { get; private set; }

		readonly DataContractSerializer _activitySerializer;


		public ActivityOverviewViewModel( Model.Laevo model )
		{
			_model = model;

			// Initialize desktop manager.
			var vdmSettings = new DefaultSettings( w =>
			{
				Process process = w.GetProcess();
				if ( process == null )
				{
					return false;
				}

				bool isLaevo = process.ProcessName.StartsWith( "Laevo" ) && w.GetClassName().Contains( "Laevo" );
				return !isLaevo;
			} );
			_desktopManager = new DesktopManager( vdmSettings );

			// Check for stored presentation options for existing activities and tasks.
			_activitySerializer = new DataContractSerializer(
				typeof( Dictionary<DateTime, ActivityViewModel> ),
				null, Int32.MaxValue, true, false,
				new ActivityDataContractSurrogate( _desktopManager ) );
			var existingActivities = new Dictionary<DateTime, ActivityViewModel>();
			if ( File.Exists( ActivitiesFile ) )
			{
				using ( var activitiesFileStream = new FileStream( ActivitiesFile, FileMode.Open ) )
				{
					existingActivities = (Dictionary<DateTime, ActivityViewModel>)_activitySerializer.ReadObject( activitiesFileStream );
				}
			}
			var existingTasks = new Dictionary<DateTime, ActivityViewModel>();
			if ( File.Exists( TasksFile ) )
			{
				using ( var tasksFileStream = new FileStream( TasksFile, FileMode.Open ) )
				{
					existingTasks = (Dictionary<DateTime, ActivityViewModel>)_activitySerializer.ReadObject( tasksFileStream );
				}
			}

			// Create home activity, which uses the first created desktop by the desktop manager.
			HomeActivity = new ActivityViewModel( this, _model.HomeActivity, _desktopManager, _desktopManager.CurrentDesktop );
			HookActivityEvents( HomeActivity );
			HomeActivity.ActivateActivity();

			// Initialize a view model for all activities from previous sessions.
			Activities = new ObservableCollection<ActivityViewModel>();
			foreach ( var activity in _model.Activities.Where( a => a != _model.HomeActivity ) )
			{
				if ( !existingActivities.ContainsKey( activity.DateCreated ) )
				{
					continue;
				}

				// Find the attention shifts which occured while the activity was open.
				ReadOnlyCollection<Interval<DateTime>> openIntervals = activity.OpenIntervals;
				var attentionShifts = _model.AttentionShifts
					.OfType<ActivityAttentionShift>()
					.Where( shift => openIntervals.Any( i => i.LiesInInterval( shift.Time ) ) );

				// Create and hook up the view model.
				var viewModel = new ActivityViewModel(
					this, activity, _desktopManager,
					existingActivities[ activity.DateCreated ],
					attentionShifts );
				HookActivityEvents( viewModel );
				Activities.Add( viewModel );
			}

			// Initialize tasks from previous sessions.
			Tasks = new ObservableCollection<ActivityViewModel>();
			// ReSharper disable ImplicitlyCapturedClosure
			var taskViewModels =
				from task in _model.Tasks
				where existingTasks.ContainsKey( task.DateCreated )
				select new ActivityViewModel(
					this, task, _desktopManager,
					existingTasks[ task.DateCreated ],
					new ActivityAttentionShift[] { } );
			// ReSharper restore ImplicitlyCapturedClosure
			foreach ( var task in taskViewModels )
			{
				HookActivityEvents( task );
				Tasks.Add( task );
			}

			// Update the activity states now that the VDM has been initialized.
			Activities.Concat( Tasks ).ForEach( a => a.UpdateHasOpenWindows() );

			// Hook up timer.
			_updateTimer.Elapsed += UpdateData;
			_updateTimer.Start();
		}


		/// <summary>
		///   Create a new activity.
		/// </summary>
		public ActivityViewModel CreateNewActivity()
		{
			var newActivity = new ActivityViewModel( this, _model.CreateNewActivity(), _desktopManager )
			{
				ShowActiveTimeSpans = _model.Settings.EnableAttentionLines
			};
			lock ( Activities )
			{
				Activities.Add( newActivity );
			}

			HookActivityEvents( newActivity );

			return newActivity;
		}

		[CommandExecute( Commands.NewTask )]
		public void NewTask()
		{
			var newTask = new ActivityViewModel( this, _model.CreateNewTask(), _desktopManager );
			lock ( Tasks )
			{
				Tasks.Add( newTask );
			}

			HookActivityEvents( newTask );
		}

		[CommandExecute( Commands.NewActivity )]
		public void NewActivity()
		{
			ActivityViewModel activity = CreateNewActivity();
			PositionActivityAtCurrentOffset( activity );
			activity.OpenActivity();
			activity.EditActivity();
		}

		[CommandExecute( Commands.PlanActivity )]
		public void PlanActivity()
		{
			ActivityViewModel activity = CreateNewActivity();
			PositionActivityAtCurrentOffset( activity );
			activity.Plan( FocusedRoundedTime );
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
			_model.Remove( activity.Activity );

			if ( Activities.Contains( activity ) )
			{
				lock ( Activities )
				{
					Activities.Remove( activity );
				}
			}
			else
			{
				lock ( Tasks )
				{
					Tasks.Remove( activity );
				}
			}

			activity.ActivatingActivityEvent -= OnActivityActivating;
			activity.ActivatedActivityEvent -= OnActivityActivated;
			activity.SelectedActivityEvent -= OnActivitySelected;
			activity.ActivityEditStartedEvent -= OnActivityEditStarted;
			activity.ActivityEditFinishedEvent -= OnActivityEditFinished;
			activity.ActivityClosedEvent -= OnActivityClosed;
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

		void HookActivityEvents( ActivityViewModel activity )
		{
			activity.ActivatingActivityEvent += OnActivityActivating;
			activity.ActivatedActivityEvent += OnActivityActivated;
			activity.SelectedActivityEvent += OnActivitySelected;
			activity.ActivityEditStartedEvent += OnActivityEditStarted;
			activity.ActivityEditFinishedEvent += OnActivityEditFinished;
			activity.ActivityClosedEvent += OnActivityClosed;
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

		void OnActivityClosed( ActivityViewModel viewModel )
		{
			CurrentActivityViewModel = null;
			ClosedActivityEvent( viewModel );
		}

		void OnActivitySelected( ActivityViewModel viewModel )
		{
			SelectedActivityEvent( viewModel );
		}

		void OnActivityEditStarted( ActivityViewModel viewModel )
		{
			ActivityMode = Mode.Edit;
		}

		void OnActivityEditFinished( ActivityViewModel viewModel )
		{
			ActivityMode = Mode.Activate;
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
			foreach ( var activity in Activities )
			{
				activity.ShowActiveTimeSpans = newIsEnabled;
			}
		}
		// ReSharper restore UnusedMember.Local
		// ReSharper restore UnusedParameter.Local

		void UpdateData( object sender, ElapsedEventArgs e )
		{
			CurrentTime = e.SignalTime;

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
		}

		public void OnOverviewActivated()
		{
			_desktopManager.UpdateWindowAssociations();
		}
		
		public void CutWindow()
		{
			_desktopManager.CutWindow( WindowManager.GetForegroundWindow() );
		}

		public void PasteWindows()
		{
			_desktopManager.PasteWindows();
		}

		public void TaskDropped( ActivityViewModel task )
		{
			// Ensure it is a task being dropped.
			if ( !Tasks.Contains( task ) )
			{
				return;
			}

			// Convert to activity.
			_model.CreateActivityFromTask( task.Activity );
			task.ShowActiveTimeSpans = _model.Settings.EnableAttentionLines;
			Tasks.Remove( task );
			Activities.Add( task );

			PositionActivityAtCurrentOffset( task );

			// Based on where the task is dropped, open, or plan it.
			if ( IsFocusedTimeBeforeNow )
			{
				task.OpenActivity();
			}
			else
			{
				task.Plan( FocusedRoundedTime );
				task.EditActivity();
			}
		}

		void PositionActivityAtCurrentOffset( ActivityViewModel task )
		{
			var offsetRange = new Interval<double>( task.HeightPercentage, 1 );
			var percentageInterval = new Interval<double>( 0, 1 );
			task.OffsetPercentage = offsetRange.Map( FocusedOffsetPercentage, percentageInterval ).Clamp( 0, 1 );
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
			// Persist activities.
			lock ( Activities )
			{
				Activities.ForEach( a => a.Persist() );
			}
			using ( var activitiesFileStream = new FileStream( ActivitiesFile, FileMode.Create ) )
			{
				_activitySerializer.WriteObject( activitiesFileStream, Activities.ToDictionary( a => a.DateCreated, a => a ) );
			}

			// Persist tasks.
			lock ( Tasks )
			{
				Tasks.ForEach( t => t.Persist() );
			}
			using ( var tasksFileStream = new FileStream( TasksFile, FileMode.Create ) )
			{
				_activitySerializer.WriteObject( tasksFileStream, Tasks.ToDictionary( t => t.DateCreated, t => t ) );
			}
		}

		protected override void FreeUnmanagedResources()
		{
			_updateTimer.Stop();
			Activities.ForEach( a => a.Dispose() );

			_desktopManager.Close();
		}
	}
}