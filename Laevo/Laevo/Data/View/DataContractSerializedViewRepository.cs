using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ABC.Applications.Persistence;
using ABC.Workspaces;
using Laevo.Data.Common;
using Laevo.Data.Model;
using Laevo.Model;
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
			public Dictionary<Guid, Dictionary<Guid, ActivityViewModel>> Activities = new Dictionary<Guid, Dictionary<Guid, ActivityViewModel>>();
		}


		readonly WorkspaceManager _workspaceManager;
		readonly IModelRepository _modelData;

		readonly string _file;
		readonly DataContractSerializer _serializer;
		readonly Data _data;
		Activity _currentVisibleParent;


		public DataContractSerializedViewRepository( string programDataFolder, WorkspaceManager workspaceManager, IModelRepository modelData, PersistenceProvider persistenceProvider )
		{
			_workspaceManager = workspaceManager;
			_modelData = modelData;
			_file = Path.Combine( programDataFolder, "ActivityRepresentations.xml" );

			// Check for stored presentation options for existing activities and tasks.
			_serializer = new DataContractSerializer(
				typeof( Data ),
				workspaceManager.GetPersistedDataTypes().Concat( persistenceProvider.GetPersistedDataTypes() ),
				Int32.MaxValue, true, false,
				new ViewDataContractSurrogate( workspaceManager ) );
			_data = new Data();
			if ( File.Exists( _file ) )
			{
				using ( var activitiesFileStream = new FileStream( _file, FileMode.Open ) )
				{
					_data = (Data)_serializer.ReadObject( activitiesFileStream );
				}
			}

			// Initialize user view model.
			User = GetUser( modelData.User );

			// Initialize home from previous session, or initialize.
			if ( _data.Home != null )
			{
				Home = new ActivityViewModel( modelData.HomeActivity, workspaceManager, this, _data.Home );
			}

			// At startup, load time line for home activity.
			LoadActivities( modelData.HomeActivity );
		}

		public override ActivityViewModel LoadActivity( Activity activity )
		{
			List<Activity> path = _modelData.GetPath( activity );
			if ( path.Count == 0 )
			{
				return Home;
			}

			Guid parentId = path.Last().Identifier;
			Dictionary<Guid, ActivityViewModel> activities = _data.Activities[ parentId ];
			ActivityViewModel storedViewModel = activities[ activity.Identifier ];
			return new ActivityViewModel( activity, _workspaceManager, this, storedViewModel );
		}

		public override sealed void LoadActivities( Activity parentActivity )
		{
			_currentVisibleParent = parentActivity;
			Activities.Clear();
			Tasks.Clear();

			Dictionary<Guid, ActivityViewModel> activities;
			if ( !_data.Activities.TryGetValue( parentActivity.Identifier, out activities ) )
			{
				activities = new Dictionary<Guid, ActivityViewModel>();
			}

			// Initialize view model for all activities for the given parent activity.
			var createViewModels =
				from activity in _modelData.GetActivities( parentActivity )
				where activities.ContainsKey( activity.Identifier )
				select new ActivityViewModel(
					activity, _workspaceManager, this,
					activities[ activity.Identifier ] );
			var viewModels = createViewModels.ToList();
			viewModels.Where( v => v.WorkIntervals.Count != 0 ).ForEach( v => Activities.Add( v ) );

			// Initialize tasks.
			// The list needs to be reversed since the tasks are stored in the correct order, but each time inserted at the start.
			var tasks = viewModels.Where( v => v.IsToDo );
			foreach ( var task in tasks.Reverse() )
			{
				Tasks.Add( task );
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

		public override List<ActivityViewModel> GetPath( ActivityViewModel activity )
		{
			List<Activity> path = _modelData.GetPath( activity.Activity );
			List<ActivityViewModel> parents = path.Select( LoadActivity ).ToList();
			parents.Add( activity );

			return parents;
		}

		public override void AddActivity( ActivityViewModel activity, Guid parent )
		{
			Dictionary<Guid, ActivityViewModel> activities;
			if ( _data.Activities.TryGetValue( parent, out activities ) )
			{
				activities.Add( activity.Identifier, activity );
				return;
			}
			_data.Activities.Add( parent, new Dictionary<Guid, ActivityViewModel> { { activity.Identifier, activity } } );
		}

		public override void RemoveActivity( ActivityViewModel activityToRemove )
		{
			foreach ( var aggregated in _data.Activities.Values )
			{
				aggregated.Remove( activityToRemove.Identifier );
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

					// Be sure to add latest activity view models to data structure.
					_data.Home = Home;
					_data.Activities[ _currentVisibleParent.Identifier ] = Activities.Union( Tasks ).ToDictionary( a => a.Identifier, a => a );

					PersistanceHelper.Persist( _file, _serializer, _data );
				}
		}
	}
}