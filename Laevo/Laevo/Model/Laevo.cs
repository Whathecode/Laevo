using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Threading;
using ABC.Interruptions;
using Laevo.Model.AttentionShifts;
using Whathecode.System;
using Whathecode.System.Extensions;
using Whathecode.System.Linq;
using Whathecode.System.Windows.Threading;


namespace Laevo.Model
{
	/// <summary>
	///   The main model of the Laevo Virtual Desktop Manager.
	///   TODO: Make tasks typesafe?
	/// </summary>
	/// <author>Steven Jeuris</author>
	class Laevo
	{
		public static readonly string ProgramName = "Laevo";
		public static readonly string ProgramDataFolder 
			= Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), ProgramName );
		static readonly string ActivitiesFile = Path.Combine( ProgramDataFolder, "Activities.xml" );
		static readonly string TasksFile = Path.Combine( ProgramDataFolder, "Tasks.xml" );
		static readonly string AttentionShiftsFile = Path.Combine( ProgramDataFolder, "AttentionShifts.xml" );
		static readonly string SettingsFile = Path.Combine( ProgramDataFolder, "Settings.xml" );
		static readonly string PluginLibrary = Path.Combine( ProgramDataFolder, "InterruptionHandlers" );

		readonly Dispatcher _dispatcher;

		readonly ProcessTracker _processTracker = new ProcessTracker();
		/// <summary>
		///   Triggered when the user returns from the logon screen to the desktop.
		///   HACK: This is required due to a bug in WPF. More info can be found in MainViewModel.RecoverFromGuiCrash().
		/// </summary>
		public event Action LogonScreenExited;

		readonly DataContractSerializer _activitySerializer;
		readonly List<Activity> _activities = new List<Activity>();
		public ReadOnlyCollection<Activity> Activities
		{
			get { return _activities.AsReadOnly(); }
		}

		public event Action<Activity> InterruptionAdded;
		readonly List<Activity> _tasks = new List<Activity>();
		public ReadOnlyCollection<Activity> Tasks
		{
			get { return _tasks.AsReadOnly(); }
		}

		public event Action<Activity> ActivityRemoved;

		public Activity HomeActivity { get; private set; }

		public Activity CurrentActivity { get; private set; }

		readonly DataContractSerializer _attentionShiftSerializer;
		readonly List<AbstractAttentionShift> _attentionShifts = new List<AbstractAttentionShift>();
		public ReadOnlyCollection<AbstractAttentionShift> AttentionShifts
		{
			get { return _attentionShifts.AsReadOnly(); }
		}

		readonly InterruptionAggregator _interruptionAggregator = new InterruptionAggregator( PluginLibrary );
		
		static readonly DataContractSerializer SettingsSerializer = new DataContractSerializer( typeof( Settings ) );
		public Settings Settings { get; private set; }


		public Laevo()
		{
			_dispatcher = Dispatcher.CurrentDispatcher;

			// Load settings.
			if ( File.Exists( SettingsFile ) )
			{
				using ( var settingsFileStream = new FileStream( SettingsFile, FileMode.Open ) )
				{
					Settings = (Settings)SettingsSerializer.ReadObject( settingsFileStream );
				}
			}
			else
			{
				Settings = new Settings();
			}

			// Initialize activity serializer.
			// It needs to be aware about the interruption types loaded by the interruption aggregator.
			_activitySerializer = new DataContractSerializer( typeof( List<Activity> ), _interruptionAggregator.GetInterruptionTypes() );

			// Add activities from previous sessions.
			if ( File.Exists( ActivitiesFile ) )
			{
				using ( var activitiesFileStream = new FileStream( ActivitiesFile, FileMode.Open ) )
				{
					var existingActivities = (List<Activity>)_activitySerializer.ReadObject( activitiesFileStream );
					existingActivities.ForEach( AddActivity );
				}
			}

			// Find home activity and set as current activity.
			HomeActivity = Activities.Count > 0
				? Activities.MinBy( a => a.DateCreated )
				: CreateNewActivity( "Home" );
			CurrentActivity = HomeActivity;

			// Add tasks from previous sessions.
			if ( File.Exists( TasksFile ) )
			{
				using ( var tasksFileStream = new FileStream( TasksFile, FileMode.Open ) )
				{
					var existingTasks = (List<Activity>)_activitySerializer.ReadObject( tasksFileStream );
					existingTasks.Reverse();  // Tasks are saved in the order they should show up, but each time added to the front of the list. Reverse to maintain the correct ordering.
					existingTasks.ForEach( AddTask );
				}
			}

			// Add attention spans from previous sessions.
			_attentionShiftSerializer = new DataContractSerializer(
				typeof( List<AbstractAttentionShift> ), new [] { typeof( ApplicationAttentionShift ), typeof( ActivityAttentionShift ) },
				int.MaxValue, true, false,
				new DataContractSurrogate( _activities.Concat( _tasks ).ToList() ) );
			if ( File.Exists( AttentionShiftsFile ) )
			{
				using ( var attentionFileStream = new FileStream( AttentionShiftsFile, FileMode.Open ) )
				{
					var existingAttentionShifts = (List<AbstractAttentionShift>)_attentionShiftSerializer.ReadObject( attentionFileStream );
					_attentionShifts.AddRange( existingAttentionShifts );
				}
			}

			// Set up interruption handlers.
			_interruptionAggregator.InterruptionReceived += interruption =>
			{
				// TODO: For now all interruptions lead to new activities, but later they might be added to existing activities.
				var newActivity = new Activity( interruption.Name );
				newActivity.AddInterruption( interruption );
				DispatcherHelper.SafeDispatch( _dispatcher, () =>
				{
					AddTask( newActivity ); 
					InterruptionAdded( newActivity );	// TODO: This event should probably be removed and some other mechanism should be used.
				} );
			};

			// Start tracking processes.
			_processTracker.Start();
			_processTracker.ProcessStopped += p =>
			{
				// TODO: Improved verification, rather than just name.
				if ( p.Name == "LogonUI.exe" )
				{
					LogonScreenExited();
				}
			};

			_attentionShifts.Add( new ApplicationAttentionShift( ApplicationAttentionShift.Application.Startup ) );
		}


		public const int SnapToMinutes = 15;
		public static DateTime GetNearestTime( DateTime near )
		{
			const int snapToMinutes = 15;

			return near.Round( DateTimePart.Minute ).SafeSubtract( TimeSpan.FromMinutes( near.Minute % snapToMinutes ) );
		}

		public void Update( DateTime now )
		{
			_activities.ForEach( a => a.Update( now ) );
			_interruptionAggregator.Update( now );
		}

		/// <summary>
		///   Create and add a new activity and sets it as the current activity.
		/// </summary>
		/// <returns>The newly created activity.</returns>
		public Activity CreateNewActivity( string name = "New Activity" )
		{
			var activity = new Activity( name );
			AddActivity( activity );

			CurrentActivity = activity;
			return activity;
		}

		void AddActivity( Activity activity )
		{
			activity.ActivatedEvent += OnActivityActivated;
			_activities.Add( activity );
		}

		void OnActivityActivated( Activity activity )
		{
			_attentionShifts.Add( new ActivityAttentionShift( activity ) );
			CurrentActivity = activity;
		}

		/// <summary>
		///   Remove a task or activity.
		/// </summary>
		/// <param name = "activity">The task or activity to remove.</param>
		public void Remove( Activity activity )
		{
			activity.ActivatedEvent -= OnActivityActivated;

			_attentionShifts.OfType<ActivityAttentionShift>().Where( s => s.Activity == activity ).ForEach( a => a.ActivityRemoved() );

			if ( _activities.Contains( activity ) )
			{
				_activities.Remove( activity );
			}
			else
			{
				_tasks.Remove( activity );
			}

			ActivityRemoved( activity );
		}

		public Activity CreateNewTask()
		{
			var task = new Activity( "New Task" );
			AddTask( task );

			return task;
		}

		void AddTask( Activity task )
		{
			task.ActivatedEvent += OnActivityActivated;
			_tasks.Insert( 0, task );
		}

		public void CreateActivityFromTask( Activity task )
		{
			// Ensure it is a task which is passed.
			if ( !_tasks.Contains( task ) )
			{
				throw new InvalidOperationException( "The passed activity is not a task from the task list." );
			}

			_tasks.Remove( task );
			_activities.Add( task );
		}

		public void SwapTaskOrder( Activity task1, Activity task2 )
		{
			_tasks.Swap( task1, task2 );
		}

		public void Exit()
		{
			_attentionShifts.Add( new ApplicationAttentionShift( ApplicationAttentionShift.Application.Shutdown ) );

			_processTracker.Stop();

			// Persist settings.
			Persist( SettingsFile, SettingsSerializer, Settings );

			// Persist activities.
			// TODO: InvalidOperationException: Collection was modified; enumeration operation may not execute.
			Persist( ActivitiesFile, _activitySerializer, _activities );

			// Persist tasks.
			Persist( TasksFile, _activitySerializer, _tasks );

			// Persist attention shifts.
			Persist( AttentionShiftsFile, _attentionShiftSerializer, _attentionShifts );
		}

		static void Persist( string file, DataContractSerializer serializer, object toSerialize )
		{
			// Make a backup first, so if serialization fails, no data is lost.
			string backupFile = file + ".backup";
			if ( File.Exists( file ) )
			{
				File.Copy( file, backupFile );
			}

			// Serialize the data.
			try
			{
				using ( var fileStream = new FileStream( file, FileMode.Create ) )
				{
					serializer.WriteObject( fileStream, toSerialize );
				}
			}
			catch ( Exception )
			{
				if ( File.Exists( backupFile ) )
				{
					File.Delete( file );
					File.Move( backupFile, file );
				}
				MessageBox.Show( "Serialization of data to file \"" + file + "\" failed. Recent data will be lost.", "Saving data failed", MessageBoxButton.OK );
			}

			// Remove temporary backup file.
			if ( File.Exists( backupFile ) )
			{
				File.Delete( backupFile );
			}
		}
	}
}
