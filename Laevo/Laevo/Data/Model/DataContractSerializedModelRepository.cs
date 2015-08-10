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
		[DataContract]
		class Data
		{
			[DataMember]
			public Activity Home;
			[DataMember]
			public List<Activity> Activities = new List<Activity>();
			[DataMember]
			public List<Activity> Tasks = new List<Activity>(); 
		}


		readonly string _activitiesFile;
		readonly string _attentionShiftsFile;
		readonly string _settingsFile;

		readonly DataContractSerializer _activitySerializer;
		readonly DataContractSerializer _attentionShiftSerializer;
		static readonly DataContractSerializer SettingsSerializer = new DataContractSerializer( typeof( Settings ) );


		public DataContractSerializedModelRepository( string programDataFolder, AbstractInterruptionAggregator interruptionAggregator )
		{
			// Set up file paths.
			_activitiesFile = Path.Combine( programDataFolder, "Activities.xml" );
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
			_activitySerializer = new DataContractSerializer( typeof( Data ), interruptionAggregator.GetInterruptionTypes() );

			// Load previous data.
			Data loadedData = new Data();
			if ( File.Exists( _activitiesFile ) )
			{
				using ( var activitiesFileStream = new FileStream( _activitiesFile, FileMode.Open ) )
				{
					loadedData = (Data)_activitySerializer.ReadObject( activitiesFileStream );
				}
			}

			// Add activities and tasks from previous sessions.
			MemoryActivities.AddRange( loadedData.Activities );
			MemoryTasks.AddRange( loadedData.Tasks );
			// TODO: Can this design be improved so implementing repositories can't forget to hook up the ToDoChangedEvent?
			MemoryActivities.Concat( MemoryTasks ).ToList().ForEach( a => a.ToDoChangedEvent += OnActivityToDoChanged );

			// Set home activity.
			if ( loadedData.Home != null )
			{
				HomeActivity = loadedData.Home;
			}
			else
			{
				HomeActivity = CreateNewActivity( "Home" );
				HomeActivity.MakeToDo();
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

			// Add attention spans from previous sessions.
			_attentionShiftSerializer = new DataContractSerializer(
				typeof( List<AbstractAttentionShift> ), new[] { typeof( ApplicationAttentionShift ), typeof( ActivityAttentionShift ) },
				int.MaxValue, true, false,
				new DataContractSurrogate( Activities.Concat( Tasks ).Concat( new [] { HomeActivity } ).ToList() ) );
			if ( File.Exists( _attentionShiftsFile ) )
			{
				using ( var attentionFileStream = new FileStream( _attentionShiftsFile, FileMode.Open ) )
				{
					var existingAttentionShifts = (List<AbstractAttentionShift>)_attentionShiftSerializer.ReadObject( attentionFileStream );
					MemoryAttentionShifts.AddRange( existingAttentionShifts );
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

			// Persist activities and tasks
			// TODO: InvalidOperationException: Collection was modified; enumeration operation may not execute.
			lock ( MemoryActivities )
			lock ( MemoryTasks )
			{
				Data data = new Data()
				{
					Home = HomeActivity,
					Activities = MemoryActivities,
					Tasks = MemoryTasks
				};
				PersistanceHelper.Persist( _activitiesFile, _activitySerializer, data );
			}

			// Persist attention shifts.
			lock ( MemoryAttentionShifts )
			{
				PersistanceHelper.Persist( _attentionShiftsFile, _attentionShiftSerializer, MemoryAttentionShifts );
			}
		}
	}
}