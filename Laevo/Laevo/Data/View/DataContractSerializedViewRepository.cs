using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ABC.Applications.Persistence;
using ABC.Workspaces;
using Laevo.Data.Common;
using Laevo.Data.Model;
using Laevo.ViewModel.Activity;
using Whathecode.System.Extensions;


namespace Laevo.Data.View
{
	/// <summary>
	///   Provides access to persisted view data of Laevo, stored in flat files serialized by DataContractSerializer.
	/// </summary>
	/// <author>Steven Jeuris</author>
	class DataContractSerializedViewRepository : AbstractMemoryViewRepository
	{
		[DataContract]
		class Data
		{
			[DataMember]
			public ActivityViewModel Home;
			[DataMember]
			public Dictionary<Guid, ActivityViewModel> Activities = new Dictionary<Guid, ActivityViewModel>();
			[DataMember]
			public Dictionary<Guid, ActivityViewModel> Tasks = new Dictionary<Guid, ActivityViewModel>();
		}


		readonly string _file;
		readonly DataContractSerializer _serializer;


		public DataContractSerializedViewRepository( string programDataFolder, WorkspaceManager workspaceManager, IModelRepository modelData, PersistenceProvider persistenceProvider )
		{
			_file = Path.Combine( programDataFolder, "ActivityRepresentations.xml" );

			// Check for stored presentation options for existing activities and tasks.
			_serializer = new DataContractSerializer(
				typeof( Data ),
				workspaceManager.GetPersistedDataTypes().Concat( persistenceProvider.GetPersistedDataTypes() ),
				Int32.MaxValue, true, false,
				new ActivityDataContractSurrogate( workspaceManager ) );
			Data loadedData = new Data();
			if ( File.Exists( _file ) )
			{
				using ( var activitiesFileStream = new FileStream( _file, FileMode.Open ) )
				{
					loadedData = (Data)_serializer.ReadObject( activitiesFileStream );
				}
			}

			// Initialize a view model for all activities from previous sessions.
			foreach ( var activity in modelData.Activities )
			{
				if ( !loadedData.Activities.ContainsKey( activity.Identifier ) )
				{
					continue;
				}

				// Create and hook up the view model.
				var viewModel = new ActivityViewModel(
					activity, workspaceManager,
					loadedData.Activities[ activity.Identifier ]);
				Activities.Add( viewModel );
			}

			// Initialize tasks from previous sessions.
			// ReSharper disable ImplicitlyCapturedClosure
			var taskViewModels =
				from task in modelData.Tasks
				where loadedData.Tasks.ContainsKey( task.Identifier )
				select new ActivityViewModel(
					task, workspaceManager,
					loadedData.Tasks[ task.Identifier ]);
			// ReSharper restore ImplicitlyCapturedClosure
			foreach ( var task in taskViewModels.Reverse() ) // The list needs to be reversed since the tasks are stored in the correct order, but each time inserted at the start.
			{
				Tasks.Add( task );
			}

			// Initialize home from previous session.
			if ( loadedData.Home != null )
			{
				Home = new ActivityViewModel( modelData.HomeActivity, workspaceManager, loadedData.Home );
			}

			// HACK: Replace duplicate activity instances in tasks with the instances found in activities.
			for ( int i = 0; i < Tasks.Count; ++i )
			{
				ActivityViewModel task = Tasks[ i ];
				ActivityViewModel activity = Activities.FirstOrDefault( a => a.Equals( task ) );
				if ( activity != null )
				{
					Tasks[ i ] = activity;
				}
			}
		}


		public override void SaveChanges()
		{
			// Persist activities and tasks.
			lock ( Activities )
			lock ( Tasks )
			{
				Activities.ForEach( a => a.Persist() );
				Tasks.ForEach( a => a.Persist() );
				var data = new Data()
				{
					Home = Home,
					Activities = Activities.ToDictionary( a => a.Identifier, a => a ),
					Tasks = Tasks.ToDictionary( t => t.Identifier, t => t )
				};
				PersistanceHelper.Persist( _file, _serializer, data );
			}
		}
	}
}