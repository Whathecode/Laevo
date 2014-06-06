using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ABC.Applications.Persistence;
using ABC.Windows.Desktop;
using Laevo.Data.Common;
using Laevo.Data.Model;
using Laevo.Model.AttentionShifts;
using Laevo.ViewModel.Activity;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;


namespace Laevo.Data.View
{
	/// <summary>
	///   Provides access to persisted view data of Laevo, stored in flat files serialized by DataContractSerializer.
	/// </summary>
	/// <author>Steven Jeuris</author>
	class DataContractSerializedViewRepository : AbstractMemoryViewRepository
	{
		readonly string _activitiesFile;
		readonly string _tasksFile;

		readonly DataContractSerializer _activitySerializer;


		public DataContractSerializedViewRepository( string programDataFolder, VirtualDesktopManager desktopManager, IModelRepository modelData, PersistenceProvider persistenceProvider )
		{
			_activitiesFile = Path.Combine( programDataFolder, "ActivityRepresentations.xml" );
			_tasksFile = Path.Combine( programDataFolder, "TaskRepresentations.xml" );

			// Check for stored presentation options for existing activities and tasks.
			_activitySerializer = new DataContractSerializer(
				typeof( Dictionary<Guid, ActivityViewModel> ),
				persistenceProvider.GetPersistedDataTypes(),
				Int32.MaxValue, true, false,
				new ActivityDataContractSurrogate( desktopManager ) );
			var existingActivities = new Dictionary<Guid, ActivityViewModel>();

			// Add activities representation from previous sessions.
			if ( File.Exists( _activitiesFile ) )
			{
				existingActivities = (Dictionary<Guid, ActivityViewModel>)PersistanceHelper.ReadData( _activitiesFile, _activitySerializer, existingActivities.GetType() );
			}
			var existingTasks = new Dictionary<Guid, ActivityViewModel>();

			// Add tasks representation from previous sessions.
			if ( File.Exists( _tasksFile ) )
			{
				existingTasks = (Dictionary<Guid, ActivityViewModel>)PersistanceHelper.ReadData( _tasksFile, _activitySerializer, existingTasks.GetType() );
			}

			// Initialize a view model for all activities from previous sessions.
			foreach ( var activity in modelData.Activities.Where( a => !a.Equals( modelData.HomeActivity ) ) )
			{
				if ( !existingActivities.ContainsKey( activity.Identifier ) )
				{
					continue;
				}

				// Find the attention shifts which occured while the activity was open.
				IReadOnlyCollection<Interval<DateTime>> openIntervals = activity.OpenIntervals;
				var attentionShifts = modelData.AttentionShifts
					.OfType<ActivityAttentionShift>()
					.Where( shift => openIntervals.Any( i => i.LiesInInterval( shift.Time ) ) );

				// Create and hook up the view model.
				var viewModel = new ActivityViewModel(
					activity, desktopManager,
					existingActivities[ activity.Identifier ],
					attentionShifts );
				Activities.Add( viewModel );
			}

			// Initialize tasks from previous sessions.
			// ReSharper disable ImplicitlyCapturedClosure
			var taskViewModels =
				from task in modelData.Tasks
				where existingTasks.ContainsKey( task.Identifier )
				select new ActivityViewModel(
					task, desktopManager,
					existingTasks[ task.Identifier ],
					new ActivityAttentionShift[] { } );
			// ReSharper restore ImplicitlyCapturedClosure
			foreach ( var task in taskViewModels.Reverse() ) // The list needs to be reversed since the tasks are stored in the correct order, but each time inserted at the start.
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


		public override void SaveChanges()
		{
			// Persist activities.
			lock ( Activities )
			{
				Activities.ForEach( a => a.Persist() );
				PersistanceHelper.Persist( _activitiesFile, _activitySerializer, Activities.ToDictionary( a => a.Identifier, a => a ) );
			}

			// Persist tasks.
			lock ( Tasks )
			{
				Tasks.ForEach( t => t.Persist() );
				PersistanceHelper.Persist( _tasksFile, _activitySerializer, Tasks.ToDictionary( a => a.Identifier, a => a ) );
			}
		}
	}
}