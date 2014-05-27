using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ABC.Interruptions;
using Laevo.Data.Common;
using Laevo.Model;
using Laevo.Model.AttentionShifts;
using Whathecode.System.Linq;


namespace Laevo.Data.Model
{
	/// <summary>
	///   Provides access to persisted model data of Laevo, stored in flat files serialized by DataContractSerializer.
	/// </summary>
	/// <author>Steven Jeuris</author>
	class DataContractSerializedModelRepository : AbstractMemoryModelRepository
	{
		readonly string _activitiesFile;
		readonly string _tasksFile;
		readonly string _attentionShiftsFile;
		readonly string _settingsFile;

		readonly DataContractSerializer _activitySerializer;
		readonly DataContractSerializer _attentionShiftSerializer;
		static readonly DataContractSerializer SettingsSerializer = new DataContractSerializer( typeof( Settings ) );


		public DataContractSerializedModelRepository( string programDataFolder, AbstractInterruptionTrigger interruptionAggregator )
		{
			// Set up file paths.
			_activitiesFile = Path.Combine( programDataFolder, "Activities.xml" );
			_tasksFile = Path.Combine( programDataFolder, "Tasks.xml" );
			_attentionShiftsFile = Path.Combine( programDataFolder, "AttentionShifts.xml" );
			_settingsFile = Path.Combine( programDataFolder, "Settings.xml" );

			// Load settings.
			if ( File.Exists( _settingsFile ) )
			{
				using ( var settingsFileStream = new FileStream( _settingsFile, FileMode.Open ) )
				{
					Settings = (Settings)SettingsSerializer.ReadObject( settingsFileStream );
				}
			}

			// Initialize activity serializer.
			// It needs to be aware about the interruption types loaded by the interruption aggregator.
			_activitySerializer = new DataContractSerializer( typeof( List<Activity> ), interruptionAggregator.GetInterruptionTypes() );

			// Add activities from previous sessions.
			if ( File.Exists( _activitiesFile ) )
			{
				using ( var activitiesFileStream = new FileStream( _activitiesFile, FileMode.Open ) )
				{
					var activities = (List<Activity>)_activitySerializer.ReadObject( activitiesFileStream );
					MemoryActivities.AddRange( activities );
					// TODO: Can this design be improved so implementing repositories can't forget to hook up the ToDoChangedEvent?
					activities.ForEach( a => a.ToDoChangedEvent += OnActivityToDoChanged );
				}
			}

			// Set home activity.
			HomeActivity = Activities.Count > 0
				? Activities.MinBy( a => a.DateCreated )
				: CreateNewActivity( "Home" );

			// Add tasks from previous sessions.
			if ( File.Exists( _tasksFile ) )
			{
				using ( var tasksFileStream = new FileStream( _tasksFile, FileMode.Open ) )
				{
					var tasks = (List<Activity>)_activitySerializer.ReadObject( tasksFileStream );
					MemoryTasks.AddRange( tasks );
					tasks.ForEach( t => t.ToDoChangedEvent += OnActivityToDoChanged );
				}
			}

			// Add attention spans from previous sessions.
			_attentionShiftSerializer = new DataContractSerializer(
				typeof( List<AbstractAttentionShift> ), new[] { typeof( ApplicationAttentionShift ), typeof( ActivityAttentionShift ) },
				int.MaxValue, true, false,
				new DataContractSurrogate( Activities.Concat( Tasks ).ToList() ) );
			if ( File.Exists( _attentionShiftsFile ) )
			{
				using ( var attentionFileStream = new FileStream( _attentionShiftsFile, FileMode.Open ) )
				{
					var existingAttentionShifts = (List<AbstractAttentionShift>)_attentionShiftSerializer.ReadObject( attentionFileStream );
					MemoryAttentionShifts.AddRange( existingAttentionShifts );
				}
			}

			// HACK: Replace duplicate activity instances in tasks with the instances found in activities.
			for ( int i = 0; i < Tasks.Count; ++i )
			{
				Activity task = MemoryTasks[ i ];
				Activity activity = MemoryActivities.FirstOrDefault( a => a.Equals( task ) );
				if ( activity != null )
				{
					MemoryTasks[ i ] = activity;
				}
			}
		}

		public override void SaveChanges()
		{
			// Persist settings.
			lock ( Settings )
			{
				PersistanceHelper.Persist( _settingsFile, SettingsSerializer, Settings );
			}

			// Persist activities.
			// TODO: InvalidOperationException: Collection was modified; enumeration operation may not execute.
			lock ( MemoryActivities )
			{
				PersistanceHelper.Persist( _activitiesFile, _activitySerializer, MemoryActivities );
			}

			// Persist tasks.
			lock ( MemoryTasks )
			{
				PersistanceHelper.Persist( _tasksFile, _activitySerializer, MemoryTasks );
			}

			// Persist attention shifts.
			lock ( MemoryAttentionShifts )
			{
				PersistanceHelper.Persist( _attentionShiftsFile, _attentionShiftSerializer, MemoryAttentionShifts );
			}
		}
	}
}