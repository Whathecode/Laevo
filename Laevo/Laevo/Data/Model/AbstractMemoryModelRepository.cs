using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		protected IPeerFactory PeerFactory { get; private set; }
		protected readonly Dictionary<Guid, List<Activity>> MemoryActivities = new Dictionary<Guid, List<Activity>>();
		protected readonly Dictionary<Activity, Guid>  ActivityParents = new Dictionary<Activity, Guid>();
		protected readonly Dictionary<Guid, Activity> ActivityGuids = new Dictionary<Guid, Activity>();

		readonly Dictionary<Activity, IActivityPeer> _activityPeers = new Dictionary<Activity, IActivityPeer>();

		public User User { get; protected set; }

		protected readonly List<AbstractAttentionShift> MemoryAttentionShifts = new List<AbstractAttentionShift>();
		public ReadOnlyCollection<AbstractAttentionShift> AttentionShifts
		{
			get { return MemoryAttentionShifts.AsReadOnly(); }
		}

		public Activity HomeActivity { get; protected set; }
		public Settings Settings { get; protected set; }


		protected AbstractMemoryModelRepository( IPeerFactory peerFactory )
		{
			PeerFactory = peerFactory;

			// Initialize activity peers for shared activities, and start listening for changes.
			GetSharedActivities().ForEach( AddActivityPeer );
			ActivityGuids.Values.ForEach( a => a.AccessAddedEvent += OnActivityAccessAdded );

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
		public List<Activity> GetPath( Activity activity )
		{
			List<Activity> parents = new List<Activity>();

			Guid parentId = ActivityParents[ activity ];
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
		///   When no parent activity is specified, the activity is added as a subactivity of <see cref="HomeActivity" />.
		/// </summary>
		/// <param name="name">Name for the newly created activity.</param>
		/// <param name="parent">The parent activity to which to add this subactivity.</param>
		/// <returns>The newly created activity.</returns>
		public Activity CreateNewActivity( string name, Activity parent = null )
		{
			var newActivity = new Activity( name, PeerFactory.GetUsersPeer() );
			newActivity.Invite( User );
			AddActivity( newActivity, parent );

			return newActivity;
		}

		public void AddActivity( Activity activity, Activity parent )
		{
			if ( ActivityGuids.ContainsKey( activity.Identifier ) )
			{
				throw new InvalidOperationException( "The passed activity is already managed by this repository." );
			}

			Guid parentId = parent == null
				? HomeActivity == null
					? Guid.Empty
					: HomeActivity.Identifier
				: parent.Identifier;
			AddActivity( activity, parentId );
		}

		protected void AddActivity( Activity activity, Guid parentId )
		{
			List<Activity> activities;
			if ( !MemoryActivities.TryGetValue( parentId, out activities ) )
			{
				activities = new List<Activity>();
				MemoryActivities[ parentId ] = activities;
			}
			activities.Add( activity );
			// TODO: Can this (if necessary?) be optimized to only listen to events if this is held in memory by the model?
			activity.AccessAddedEvent += OnActivityAccessAdded;
			activity.AccessRemovedEvent += OnActivityAccessRemoved;

			ActivityGuids.Add( activity.Identifier, activity );
			ActivityParents.Add( activity, parentId );
		}

		public void RemoveActivity( Activity activity )
		{
			foreach ( var activities in MemoryActivities.Values )
			{
				activities.Remove( activity );
			}
			activity.AccessAddedEvent -= OnActivityAccessAdded;
			activity.AccessRemovedEvent -= OnActivityAccessRemoved;

			ActivityGuids.Remove( activity.Identifier );
			ActivityParents.Remove( activity );
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

		void OnActivityAccessAdded( Activity activity, User user )
		{
			AddActivityPeer( activity );
		}

		void OnActivityAccessRemoved( Activity activity, User user )
		{
			if ( activity.AccessUsers.Count <= 1 ) // Only shared with self.
			{
				_activityPeers.Remove( activity );
			}
		}

		void AddActivityPeer( Activity activity )
		{
			if ( !_activityPeers.ContainsKey( activity ) )
			{
				_activityPeers[ activity ] = PeerFactory.GetActivityPeer( activity );
			}
		}

		public abstract void SaveChanges();
	}
}