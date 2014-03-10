using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ABC.Applications.Persistence;
using ABC.Windows.Desktop;
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
				typeof( Dictionary<DateTime, ActivityViewModel> ),
				persistenceProvider.GetPersistedDataTypes(),
				Int32.MaxValue, true, false,
				new ActivityDataContractSurrogate( desktopManager ) );
			var existingActivities = new Dictionary<DateTime, ActivityViewModel>();
			if ( File.Exists( _activitiesFile ) )
			{
				using ( var activitiesFileStream = new FileStream( _activitiesFile, FileMode.Open ) )
				{
					existingActivities = (Dictionary<DateTime, ActivityViewModel>)_activitySerializer.ReadObject( activitiesFileStream );
				}
			}
			var existingTasks = new Dictionary<DateTime, ActivityViewModel>();
			if ( File.Exists( _tasksFile ) )
			{
				using ( var tasksFileStream = new FileStream( _tasksFile, FileMode.Open ) )
				{
					existingTasks = (Dictionary<DateTime, ActivityViewModel>)_activitySerializer.ReadObject( tasksFileStream );
				}
			}

			// Initialize a view model for all activities from previous sessions.
			foreach ( var activity in modelData.Activities.Where( a => a != modelData.HomeActivity ) )
			{
				if ( !existingActivities.ContainsKey( activity.DateCreated ) )
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
					existingActivities[ activity.DateCreated ],
					attentionShifts );
				Activities.Add( viewModel );
			}

			// Initialize tasks from previous sessions.
			// ReSharper disable ImplicitlyCapturedClosure
			var taskViewModels =
				from task in modelData.Tasks
				where existingTasks.ContainsKey( task.DateCreated )
				select new ActivityViewModel(
					task, desktopManager,
					existingTasks[ task.DateCreated ],
					new ActivityAttentionShift[] { } );
			// ReSharper restore ImplicitlyCapturedClosure
			foreach ( var task in taskViewModels.Reverse() ) // The list needs to be reversed since the tasks are stored in the correct order, but each time inserted at the start.
			{
				Tasks.Add( task );
			}
		}


		public override void SaveChanges()
		{
			// Persist activities.
			lock ( Activities )
			{
				Activities.ForEach( a => a.Persist() );
			}
			using ( var activitiesFileStream = new FileStream( _activitiesFile, FileMode.Create ) )
			{
				_activitySerializer.WriteObject( activitiesFileStream, Activities.ToDictionary( a => a.DateCreated, a => a ) );
			}

			// Persist tasks.
			lock ( Tasks )
			{
				Tasks.ForEach( t => t.Persist() );
			}
			using ( var tasksFileStream = new FileStream( _tasksFile, FileMode.Create ) )
			{
				_activitySerializer.WriteObject( tasksFileStream, Tasks.ToDictionary( t => t.DateCreated, t => t ) );
			}
		}
	}
}
