﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ABC.Interruptions;
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
					MemoryActivities.AddRange( (List<Activity>)_activitySerializer.ReadObject( activitiesFileStream ) );
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
					MemoryTasks.AddRange( (List<Activity>)_activitySerializer.ReadObject( tasksFileStream ) );
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
		}


		public override void SaveChanges()
		{
			// Persist settings.
			Persist( _settingsFile, SettingsSerializer, Settings );

			// Persist activities.
			// TODO: InvalidOperationException: Collection was modified; enumeration operation may not execute.
			Persist( _activitiesFile, _activitySerializer, MemoryActivities );

			// Persist tasks.
			Persist( _tasksFile, _activitySerializer, MemoryTasks );

			// Persist attention shifts.
			Persist( _attentionShiftsFile, _attentionShiftSerializer, MemoryAttentionShifts );
		}

		static void Persist( string file, XmlObjectSerializer serializer, object toSerialize )
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
			catch ( Exception e )
			{
				if ( File.Exists( backupFile ) )
				{
					File.Delete( file );
					File.Move( backupFile, file );
				}
				throw new PersistenceException( "Serialization of data to file \"" + file + "\" failed. Recent data will be lost.", e );
			}

			// Remove temporary backup file.
			if ( File.Exists( backupFile ) )
			{
				File.Delete( backupFile );
			}
		}
	}
}