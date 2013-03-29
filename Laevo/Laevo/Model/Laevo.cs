using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Laevo.Model.AttentionShifts;
using Whathecode.System.Extensions;
using Whathecode.System.Linq;


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

		readonly ProcessTracker _processTracker = new ProcessTracker();
		/// <summary>
		///   Triggered when the user returns from the logon screen to the desktop.
		///   HACK: This is required due to a bug in WPF. More info can be found in MainViewModel.RecoverFromGuiCrash().
		/// </summary>
		public event Action LogonScreenExited;

		static readonly DataContractSerializer ActivitySerializer = new DataContractSerializer( typeof( List<Activity> ) );
		readonly List<Activity> _activities = new List<Activity>();
		public ReadOnlyCollection<Activity> Activities
		{
			get { return _activities.AsReadOnly(); }
		}

		readonly List<Activity> _tasks = new List<Activity>();
		public ReadOnlyCollection<Activity> Tasks
		{
			get { return _tasks.AsReadOnly(); }
		}

		public Activity HomeActivity { get; private set; }

		public Activity CurrentActivity { get; private set; }

		readonly DataContractSerializer _attentionShiftSerializer;
		readonly List<AbstractAttentionShift> _attentionShifts = new List<AbstractAttentionShift>();
		public ReadOnlyCollection<AbstractAttentionShift> AttentionShifts
		{
			get { return _attentionShifts.AsReadOnly(); }
		}
		
		static readonly DataContractSerializer SettingsSerializer = new DataContractSerializer( typeof( Settings ) );
		public Settings Settings { get; private set; }


		public Laevo()
		{
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

			// Add activities from previous sessions.
			if ( File.Exists( ActivitiesFile ) )
			{
				using ( var activitiesFileStream = new FileStream( ActivitiesFile, FileMode.Open ) )
				{
					var existingActivities = (List<Activity>)ActivitySerializer.ReadObject( activitiesFileStream );
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
					var existingTasks = (List<Activity>)ActivitySerializer.ReadObject( tasksFileStream );
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


		public void Update( DateTime now )
		{
			_activities.ForEach( a => a.Update( now ) );
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
			_tasks.Add( task );
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

		public void Exit()
		{
			_attentionShifts.Add( new ApplicationAttentionShift( ApplicationAttentionShift.Application.Shutdown ) );

			_processTracker.Stop();

			// Persist settings.
			using ( var settingsFileStream = new FileStream( SettingsFile, FileMode.Create ) )
			{
				SettingsSerializer.WriteObject( settingsFileStream, Settings );
			}

			// Persist activities.
			using ( var activitiesFileStream = new FileStream( ActivitiesFile, FileMode.Create ) )
			{
				// TODO: InvalidOperationException: Collection was modified; enumeration operation may not execute.
				ActivitySerializer.WriteObject( activitiesFileStream, _activities );
			}

			// Persist tasks.
			using ( var tasksFileStream = new FileStream( TasksFile, FileMode.Create ) )
			{
				ActivitySerializer.WriteObject( tasksFileStream, _tasks );
			}

			// Persist attention shifts.
			using ( var attentionFileStream = new FileStream( AttentionShiftsFile, FileMode.Create ) )
			{
				_attentionShiftSerializer.WriteObject( attentionFileStream, _attentionShifts );
			}
		}
	}
}
