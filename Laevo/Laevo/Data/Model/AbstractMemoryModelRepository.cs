using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using ABC.Interruptions;
using Laevo.Model;
using Laevo.Model.AttentionShifts;
using Laevo.Peer;
using Whathecode.System.Extensions;


namespace Laevo.Data.Model
{
	/// <summary>
	///   An abstract class which provides model data for Laevo which is held in memory.
	/// </summary>
	/// <author>Steven Jeuris</author>
	abstract class AbstractMemoryModelRepository : IModelRepository
	{
		protected AbstractPeerFactory PeerFactory { get; private set; }
		protected readonly Dictionary<Guid, List<Activity>> MemoryActivities = new Dictionary<Guid, List<Activity>>();
		protected readonly Dictionary<Activity, Guid> ActivityParents = new Dictionary<Activity, Guid>();
		protected readonly Dictionary<Guid, Activity> ActivityGuids = new Dictionary<Guid, Activity>();

		public User User { get; protected set; }

		protected readonly List<AbstractAttentionShift> MemoryAttentionShifts = new List<AbstractAttentionShift>();
		public ReadOnlyCollection<AbstractAttentionShift> AttentionShifts
		{
			get { return MemoryAttentionShifts.AsReadOnly(); }
		}

		public Activity HomeActivity { get; protected set; }
		public Settings Settings { get; protected set; }


		protected AbstractMemoryModelRepository( AbstractPeerFactory peerFactory )
		{
			PeerFactory = peerFactory;

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
		///   Gets all activities over which the current user has claimed ownership.
		/// </summary>
		public IEnumerable<Activity> GetPersonalActivities()
		{
			return ActivityGuids.Values.Where( a => a.OwnedUsers.Contains( User ) && !a.Equals( HomeActivity ) );
		}

		/// <summary>
		///   Returns a list of all activities which are shared.
		/// </summary>
		public IEnumerable<Activity> GetSharedActivities()
		{
			return ActivityGuids.Values.Where( a => a.AccessUsers.Count > 1 ); // All activities shared with more than self.
		}

		/// <summary>
		///   Gets the full path (parent activities) of this activity.
		/// </summary>
		/// <param name="activity">The activity to get the path for.</param>
		/// <returns>An ordered list of parent activities, where the last activity is the closest parent.</returns>
		/// <exception cref="InvalidOperationException">Thrown when activity is not within the repository.</exception>
		public List<Activity> GetPath( Activity activity )
		{
			List<Activity> parents = new List<Activity>();

			Guid parentId;
			if ( !ActivityParents.TryGetValue( activity, out parentId ) )
			{
				string error = string.Format( "The passed activity ({0}) is not managed by this repository.", activity.Name );
				throw new InvalidOperationException( error );
			}
			while ( parentId != Guid.Empty )
			{
				var parent = ActivityGuids[ parentId ];
				parents.Add( parent );
				parentId = ActivityParents[ parent ];
			}

			parents.Reverse();
			return parents;
		}

		/// <summary>
		///   Gets a full list of all unattended interruptions per activity.
		/// </summary>
		public Dictionary<Activity, List<AbstractInterruption>> GetUnattendedInterruptions()
		{
			return ActivityGuids.Values
				.Where( a => a.Interruptions.Count( i => !i.AttendedTo ) > 0 )
				.ToDictionary( a => a, a => a.Interruptions.ToList() );
		}

		/// <summary>
		///   Create a new activity with the specified name, and add it as a subactivity of the given activity.
		/// </summary>
		/// <param name="name">Name for the newly created activity.</param>
		/// <param name="parent">The parent activity to which to add this subactivity, or null when a root.</param>
		/// <returns>The newly created activity.</returns>
		public Activity CreateNewActivity( string name, Activity parent = null )
		{
			var newActivity = new Activity( name, this, PeerFactory.UsersPeer );
			AddActivity( newActivity, parent );

			// Give access to all users that have access higher up the hierarchy.
			newActivity.GetInheritedAccessUsers().ForEach( u => newActivity.Invite( u ) );

			return newActivity;
		}

		public void AddActivity( Activity activity, Activity parent )
		{
			if ( ActivityGuids.ContainsKey( activity.Identifier ) )
			{
				throw new InvalidOperationException( "The passed activity is already managed by this repository." );
			}

			Guid parentId = parent == null
				? Guid.Empty
				: parent.Identifier;
			AddActivity( activity, parentId );
		}

		void AddActivity( Activity activity, Guid parentId )
		{
			List<Activity> activities;
			if ( !MemoryActivities.TryGetValue( parentId, out activities ) )
			{
				activities = new List<Activity>();
				MemoryActivities[ parentId ] = activities;
			}
			activities.Add( activity );

			ActivityGuids.Add( activity.Identifier, activity );
			ActivityParents.Add( activity, parentId );

			// TODO: For now, this assumes all activities are held in memory. If not, peers wouldn't be started for 'unloaded' activities.
			//       A more scalable approach could load only part of the tree, but ensure loading the shared parts of the tree using e.g. GetSharedActivities().
			PeerFactory.ManageActivity( activity, GetPath( activity ) );
		}

		public void RemoveActivity( Activity activity )
		{
			Guid parent = MemoryActivities.First( m => m.Value.Contains( activity ) ).Key;
			RemoveActivity( activity, parent, true );

			// Notify peer that the activity no longer needs to be managed.
			PeerFactory.UnmanageActivity( activity );
		}

		void RemoveActivity( Activity activity, Guid parent, bool removeChildren )
		{
			// Remove from parent/children collection.
			List<Activity> children = MemoryActivities[ parent ];
			if ( children.Remove( activity ) ) // Activity can only be attached to one parent.
			{
				if ( children.Count == 0 )
				{
					MemoryActivities.Remove( parent );
				}
			}

			// Remove quick access collections.
			ActivityGuids.Remove( activity.Identifier );
			ActivityParents.Remove( activity );

			// Remove children.
			if ( removeChildren && MemoryActivities.TryGetValue( activity.Identifier, out children ) )
			{
				foreach ( Activity child in children.ToList() )
				{
					RemoveActivity( child, activity.Identifier, true );
				}
			}
		}

		public void MoveActivity( Activity activity, Activity destination )
		{
			Contract.Requires( activity != null && destination != null );

			Guid parent = MemoryActivities.First( m => m.Value.Contains( activity ) ).Key;
			RemoveActivity( activity, parent, false ); // Do not remove children, as the activity in its whole is being moved.
			AddActivity( activity, destination );

			// Notify peer that the activity has been moved.
			PeerFactory.ManageActivity( activity, GetPath( activity ) );
		}

		public bool ContainsActivity( Activity activity )
		{
			return ActivityGuids.ContainsKey( activity.Identifier );
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