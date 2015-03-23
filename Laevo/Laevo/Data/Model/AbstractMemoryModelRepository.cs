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
		protected readonly Dictionary<Guid, List<Activity>> MemoryActivities = new Dictionary<Guid, List<Activity>>();

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


		/// <summary>
		///   Get the subactivities of a certain parent activity, or the subactivities of the home activity when null.
		/// </summary>
		/// <param name="parent">The parent activity to get subactivities of.</param>
		public IEnumerable<Activity> GetActivities( Activity parent = null )
		{
			Guid parentId = parent == null ? HomeActivity.Identifier : parent.Identifier;

			List<Activity> activities;
			if ( MemoryActivities.TryGetValue( parentId, out activities ) )
			{
				return activities;
			}
			
			return new List<Activity>();
		}

		/// <summary>
		///   Create a new activity with the specified name, and add it as a subactivity of the given activity.
		///   When no parent activity is specified, the activity is added as a subactivity of <see cref="HomeActivity" />.
		/// </summary>
		/// <param name="name">Name for the newly created activity.</param>
		/// <param name="parent">The parent activity to which to add this subactivity.</param>
		/// <returns>The newly created activity.</returns>
		public Activity CreateNewActivity( string name, Activity parent = null )
		{
			var newActivity = new Activity( name );

			Guid parentId = parent == null
				? HomeActivity == null
					? Guid.Empty
					: HomeActivity.Identifier
				: parent.Identifier;
			List<Activity> activities;
			if ( !MemoryActivities.TryGetValue( parentId, out activities ) )
			{
				activities = new List<Activity>();
				MemoryActivities[ parentId ] = activities;
			}
			activities.Add( newActivity );

			return newActivity;
		}

		public void RemoveActivity( Activity activity )
		{
			foreach ( var activities in MemoryActivities.Values )
			{
				activities.Remove( activity );
			}
		}

		public void SwapTaskOrder( Activity task1, Activity task2 )
		{
			foreach ( var activities in MemoryActivities.Values )
			{
				if ( activities.Contains( task1 ) && activities.Contains( task2 ) )
				{
					activities.Swap( task1, task2 );
					return;
				}
			}
			
			throw new InvalidOperationException( "The passed activities do not belong to the same parent, and thus can not be ordered." );
		}

		public void AddAttentionShift( AbstractAttentionShift attentionShift )
		{
			MemoryAttentionShifts.Add( attentionShift );
		}

		public abstract void SaveChanges();
	}
}