using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Laevo.Model;
using Laevo.Model.AttentionShifts;
using Whathecode.System.Extensions;


namespace Laevo.Data.Model
{
	/// <summary>
	///   An abstract class which provides model data for Laevo which is held in memory.
	/// </summary>
	/// <author>Steven Jeuris</author>
	abstract class AbstractMemoryModelRepository : IModelRepository
	{
		protected readonly List<Activity> MemoryActivities = new List<Activity>();
		public ReadOnlyCollection<Activity> Activities
		{
			get { return MemoryActivities.AsReadOnly(); }
		}

		protected readonly List<Activity> MemoryTasks = new List<Activity>(); 
		public ReadOnlyCollection<Activity> Tasks
		{
			get { return MemoryTasks.AsReadOnly(); }
		}

		protected readonly List<AbstractAttentionShift> MemoryAttentionShifts = new List<AbstractAttentionShift>(); 
		public ReadOnlyCollection<AbstractAttentionShift> AttentionShifts
		{
			get { return MemoryAttentionShifts.AsReadOnly(); }
		}

		public Activity HomeActivity { get; protected set; }
		public Settings Settings { get; protected set; }


		protected AbstractMemoryModelRepository()
		{
			// Initialize settings by default to prevent extending classes from forgetting to initialize default settings.
			Settings = new Settings();
		}


		public Activity CreateNewActivity( string name )
		{
			var newActivity = new Activity( name );
			MemoryActivities.Add( newActivity );

			return newActivity;
		}

		public Activity CreateNewTask( string name )
		{
			var newTask = new Activity( name );
			MemoryTasks.Add( newTask );

			return newTask;
		}

		public void CreateActivityFromTask( Activity task )
		{
			// Ensure it is a task which is passed.
			if ( !MemoryTasks.Contains( task ) )
			{
				throw new InvalidOperationException( "The passed activity is not a task from the task list." );
			}

			MemoryTasks.Remove( task );
			MemoryActivities.Add( task );
		}

		public void RemoveActivity( Activity activity )
		{
			if ( MemoryActivities.Contains( activity ) )
			{
				MemoryActivities.Remove( activity );
			}
			else
			{
				MemoryTasks.Remove( activity );
			}
		}

		public void SwapTaskOrder( Activity task1, Activity task2 )
		{
			MemoryTasks.Swap( task1, task2 );
		}

		public void AddAttentionShift( AbstractAttentionShift attentionShift )
		{
			MemoryAttentionShifts.Add( attentionShift );
		}


		public abstract void SaveChanges();
	}
}
