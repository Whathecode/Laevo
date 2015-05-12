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

		/// <summary>
		///   True when hierarchy activities have been loaded (LoadActivities), and false when personal activities have been loaded (LoadPersonalActivities).
		/// </summary>
		bool _loadedHierarchy;
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
			// Store changes when switching from personal activities.
			// TODO: Cleanup, this is most likely temporary code once personal activities have alternate viewmodels.
			if ( !_loadedHierarchy )
			{
				foreach ( var activity in InnerActivities.Union( InnerTasks ) )
				{
					List<Activity> path = _modelData.GetPath( activity.Activity );
					if ( path.Count == 0 )
					{
						break; // Home, no changes need to be stored.
					}
					Guid parentId = path.Last().Identifier;
					Dictionary<Guid, ActivityViewModel> container = _data.Activities[ parentId ];
					container[ activity.Identifier ] = activity;
				}
			}

			_loadedHierarchy = true;
			_currentVisibleParent = parentActivity;

			Dictionary<Guid, ActivityViewModel> activities;
			if ( !_data.Activities.TryGetValue( parentActivity.Identifier, out activities ) )
			{
				activities = new Dictionary<Guid, ActivityViewModel>();
			}

			// Initialize view model for all activities and tasks for the given parent activity.
			var createViewModels =
				from activity in _modelData.GetActivities( parentActivity )
				where activities.ContainsKey( activity.Identifier )
				select new ActivityViewModel(
					activity, _workspaceManager, this,
					activities[ activity.Identifier ] );
			var viewModels = createViewModels.ToList();
			LoadViewModels( viewModels );

			// Update activities in data storage.
			_data.Activities[ _currentVisibleParent.Identifier ] = Activities.Union( Tasks ).ToDictionary( a => a.Identifier, a => a );
		}

		public override void LoadPersonalActivities()
		{
			_loadedHierarchy = false;

			// Load all personal activities.
			// TODO: Store separate presentation for personal activities which allow for different y-position.
			var createViewModels = _modelData.GetPersonalActivities().Select( LoadActivity ).ToList();
			LoadViewModels( createViewModels );
		}

		void LoadViewModels( List<ActivityViewModel> viewModels )
		{
			InnerActivities.Clear();
			InnerTasks.Clear();

			viewModels.Where( v => v.WorkIntervals.Count != 0 ).ForEach( v => InnerActivities.Add( v ) );

			// Initialize tasks.
			// The list needs to be reversed since the tasks are stored in the correct order, but each time inserted at the start.
			var tasks = viewModels.Where( v => v.IsToDo );
			foreach ( var task in tasks.Reverse() )
			{
				InnerTasks.Add( task );
			}

			// HACK: Replace duplicate activity instances in tasks with the instances found in activities.
			for ( int i = 0; i < Tasks.Count; ++i )
			{
				ActivityViewModel task = Tasks[ i ];
				ActivityViewModel activity = Activities.FirstOrDefault( a => a.Equals( task ) );
				if ( activity != null )
				{
					InnerTasks[ i ] = activity;
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

		public override void AddActivity( ActivityViewModel activity, ActivityViewModel toParent = null )
		{
			// TODO: Throw exception when activity is already managed by repository.

			if ( toParent == null )
			{
				toParent = Home;
			}

			// Add activity to data collection.
			Guid parent = toParent.Identifier;
			Dictionary<Guid, ActivityViewModel> activities;
			if ( !_data.Activities.TryGetValue( parent, out activities ) )
			{
				activities = new Dictionary<Guid, ActivityViewModel>();
				_data.Activities.Add( parent, activities );
			}
			activities.Add( activity.Identifier, activity );

			// Add activity to observable collection when its parent is currently visible in hierarchy view, or ownership is claimed in personal view.
			bool hierarchyAndVisible = _loadedHierarchy && _currentVisibleParent.Equals( toParent.Activity );
			bool personalAndOwned = !_loadedHierarchy && activity.OwnedUsers.Contains( User );
			if ( hierarchyAndVisible || personalAndOwned )
			{
				if ( activity.IsToDo )
				{
					InnerTasks.Insert( 0, activity );
				}
				else
				{
					InnerActivities.Add( activity );
				}
			}
		}

		public override void RemoveActivity( ActivityViewModel activity )
		{
			// TODO: Remove subactivities? As is now, removed subactivities which aren't added again are still stored in the repository.
			foreach ( var aggregated in _data.Activities.Values )
			{
				aggregated.Remove( activity.Identifier );
			}

			// Since it might be removed from the currently visible time line, remove from observable collections.
			InnerActivities.Remove( activity );
			InnerTasks.Remove( activity );
		}

		public override void MoveActivity( ActivityViewModel activity, ActivityViewModel toParent )
		{
			RemoveActivity( activity );
			AddActivity( activity, toParent );
		}

		public override void UpdateActivity( ActivityViewModel activity )
		{
			bool isTurnedIntoToDo = activity.Activity.IsToDo;
			if ( isTurnedIntoToDo )
			{
				InnerTasks.Insert( 0, activity );
			}
			else
			{
				InnerTasks.Remove( activity );
				if ( !InnerActivities.Contains( activity ) ) // Activity can already have a presentation on the time line when it was converted to a to do item before.
				{
					InnerActivities.Add( activity );
				}
			}
		}

		public override void SwapTaskOrder( ActivityViewModel task1, ActivityViewModel task2 )
		{
			int draggedIndex = Tasks.IndexOf( task1 );
			int currentIndex = Tasks.IndexOf( task2 );
			var reordered = Tasks
				.Select( ( t, i ) => i == draggedIndex ? currentIndex : i == currentIndex ? draggedIndex : i )
				.Select( toAdd => Tasks[ toAdd ] )
				.ToArray();
			InnerTasks.Clear();
			reordered.ForEach( InnerTasks.Add );
		}

		public override void SaveChanges()
		{
			// Persist activities and tasks.
			Activities.ForEach( a => a.Persist() );
			Tasks.ForEach( a => a.Persist() );

			// Be sure to add latest activity view models to data structure.
			_data.Home = Home;

			PersistanceHelper.Persist( _file, _serializer, _data );
		}
	}
}