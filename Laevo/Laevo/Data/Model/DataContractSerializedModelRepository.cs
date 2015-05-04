using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ABC.Interruptions;
using Laevo.Data.Common;
using Laevo.Model;
using Laevo.Model.AttentionShifts;
using Laevo.Peer;


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
			public User User;
			[DataMember]
			public Activity Home;
			[DataMember]
			public Dictionary<Guid, List<Activity>> Activities = new Dictionary<Guid, List<Activity>>();
		}


		readonly string _activitiesFile;
		readonly string _attentionShiftsFile;
		readonly string _settingsFile;

		readonly DataContractSerializer _activitySerializer;
		readonly DataContractSerializer _attentionShiftSerializer;
		static readonly DataContractSerializer SettingsSerializer = new DataContractSerializer( typeof( Settings ) );


		public DataContractSerializedModelRepository( string programDataFolder, AbstractInterruptionTrigger interruptionAggregator, AbstractPeerFactory peerFactory )
			: base( peerFactory )
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
			var modelSurrogate = new ModelDataContractSurrogate( this, peerFactory.UsersPeer );
			var surrogateTypes = new Collection<Type>();
			modelSurrogate.GetKnownCustomDataTypes( surrogateTypes );
			_activitySerializer = new DataContractSerializer(
				typeof( Data ),
				interruptionAggregator.GetInterruptionTypes().Concat( surrogateTypes ),
				Int32.MaxValue, true, false,
				modelSurrogate );

			// Load previous data.
			Data loadedData = new Data();
			if ( File.Exists( _activitiesFile ) )
			{
				using ( var activitiesFileStream = new FileStream( _activitiesFile, FileMode.Open ) )
				{
					loadedData = (Data)_activitySerializer.ReadObject( activitiesFileStream );
				}
			}

			// Set user.
			User = loadedData.User ?? new User();

			// Add activities from previous sessions.
			var processActivities = new Queue<KeyValuePair<Guid, List<Activity>>>( loadedData.Activities );
			while ( processActivities.Count > 0 )
			{
				// Check whether the parent of the dequeued element has already been added. If not, readd to queue to wait until it has been added.
				var activities = processActivities.Dequeue();
				if ( activities.Key != Guid.Empty && !ActivityGuids.ContainsKey( activities.Key ) )
				{
					processActivities.Enqueue( activities );
					continue;
				}

				// Add all subactivities of the parent.
				Activity parent = activities.Key == Guid.Empty ? null : ActivityGuids[ activities.Key ];
				foreach ( var activity in activities.Value )
				{
					AddActivity( activity, parent );
				}
			}

			// Set home activity.
			if ( loadedData.Home != null )
			{
				HomeActivity = loadedData.Home;
			}
			else
			{
				HomeActivity = CreateNewActivity( "Home" );
				HomeActivity.Invite( User ); // The home activity is owned by current user.
				HomeActivity.MakeToDo();
			}

			// Add attention spans from previous sessions.
			// TODO: Store attention spans per 'parent' activity? Deserializing this at the moment is not very scaleable.
			var allActivities = MemoryActivities.Values.SelectMany( a => a );
			_attentionShiftSerializer = new DataContractSerializer(
				typeof( List<AbstractAttentionShift> ), new[] { typeof( ApplicationAttentionShift ), typeof( ActivityAttentionShift ) },
				int.MaxValue, true, false,
				new DataContractSurrogate( allActivities.ToList() ) );
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
			{
				var data = new Data
				{
					User = User,
					Home = HomeActivity,
					Activities = MemoryActivities
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