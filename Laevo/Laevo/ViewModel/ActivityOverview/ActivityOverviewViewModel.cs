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
using Whathecode.System;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;
using Whathecode.System.Windows.Interop;
using Whathecode.VirtualDesktopManagerAPI;


namespace Laevo.ViewModel.ActivityOverview
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class ActivityOverviewViewModel : AbstractViewModel
	{
		static readonly string ActivitiesFile = Path.Combine( Model.Laevo.ProgramDataFolder, "ActivityRepresentations.xml" );
		static readonly string TasksFile = Path.Combine( Model.Laevo.ProgramDataFolder, "TaskRepresentations.xml" );


		/// <summary>
		///   Event which is triggered when an activity is opened.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler ActivatedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is selected.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler SelectedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is closed.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler ClosedActivityEvent;

		readonly Model.Laevo _model;
		readonly DesktopManager _desktopManager = new DesktopManager();

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

			// Setup desktop manager.
			_desktopManager.AddWindowFilter(
				w =>
				{
					Process process = w.GetProcess();
					return process != null && !(process.ProcessName.StartsWith( "Laevo" ) && w.GetClassName().Contains( "Laevo" ));
				} );

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

			// Ensure current (first) activity is assigned to the correct desktop.
			HomeActivity = new ActivityViewModel( this, _model.HomeActivity, _desktopManager );
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

			// Open activities which have windows assigned to them at startup so it seems as if those sessions simply continue since when the application was closed.
			Activities.Where( a => a.UpdateHasOpenWindows() ).ForEach( a => a.OpenActivity() );

			// Hook up timer.
			_updateTimer.Elapsed += UpdateData;
			_updateTimer.Start();
		}


		/// <summary>
		///   Create a new activity and open it.
		/// </summary>
		public void NewActivity()
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
			newActivity.ActivateActivity();
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
			CurrentActivityViewModel = viewModel;
			ActivatedActivityEvent( viewModel );
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
			HomeActivity.ActivateActivity();
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

		public void TaskDropped( ActivityViewModel task, DateTime atTime )
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

			// Snap time to 15 minutes.
			atTime = atTime.Round( DateTimePart.Minute ).SafeSubtract( TimeSpan.FromMinutes( atTime.Minute % 15 ) );

			// Based on where the task is dropped, open, or plan it.
			if ( atTime <= DateTime.Now )
			{
				task.OpenActivity();
			}
			else
			{
				task.Plan( atTime );
				task.EditActivity();

			}
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