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
			newActivity.ToDoChangedEvent += OnActivityToDoChanged;

			MemoryActivities.Add( newActivity );

			return newActivity;
		}

		public void RemoveActivity( Activity activity )
		{
			activity.ToDoChangedEvent -= OnActivityToDoChanged;

			MemoryActivities.Remove( activity );
			MemoryTasks.Remove( activity );
		}

		protected void OnActivityToDoChanged( Activity activity )
		{
			if ( activity.IsToDo )
			{
				MemoryTasks.Add( activity );
			}
			else
			{
				MemoryTasks.Remove( activity );
				if ( !MemoryActivities.Contains( activity ) )
				{
					MemoryActivities.Add( activity );
				}
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