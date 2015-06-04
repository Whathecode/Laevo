using System.Collections.Generic;
using System.Linq;
using Laevo.Model;
using Whathecode.System.Collections.Generic;
using Whathecode.System.Linq;


namespace Laevo.Peer
{
	public abstract class AbstractPeerFactory
	{
		class CloudNode
		{
			public readonly Activity Activity;
			public IActivityPeer Peers;


			public CloudNode( Activity activity )
			{
				Activity = activity;
			}


			public override bool Equals( object obj )
			{
				var node = obj as CloudNode;
				return node != null && Activity.Equals( node.Activity );
			}

			public override int GetHashCode()
			{
				return Activity.GetHashCode();
			}
		}

		public abstract IUsersPeer UsersPeer { get; }


		readonly Dictionary<Activity, Tree<CloudNode>> _activityNodes = new Dictionary<Activity, Tree<CloudNode>>();
		readonly List<Tree<CloudNode>> _activityPeers = new List<Tree<CloudNode>>();

        protected AbstractPeerFactory()
        {
            ServiceLocator.GetInstance().RegisterService( this );
        }

		/// <summary>
		///   Called whenever a new activity needs to be managed, or an existing activity has been moved.
		/// </summary>
		/// <param name="activity">The activity to manage.</param>
		/// <param name="path">To path from the start of the hierarchy to where the activity is located.</param>
		public void ManageActivity( Activity activity, List<Activity> path )
		{
			// Check whether passed activity is a previously managed activity which has been moved.
			Tree<CloudNode> existingNode;
			if ( _activityNodes.TryGetValue( activity, out existingNode ) )
			{
				UnmanageActivity( activity );
			}

			// Add activity to tree.
			Activity root = path.FirstOrDefault() ?? activity;
			Tree<CloudNode> newTree = _activityPeers.FirstOrDefault( p => p.Value.Activity.Equals( root ) );
			if ( newTree == null )
			{
				newTree = new Tree<CloudNode>( new CloudNode( activity ) );
				_activityPeers.Add( newTree );
			}
			else
			{
				var modifiedBranch = path.Skip( 1 ).ConcatItem( activity ); // Skip root, and add last item.
				newTree = newTree.AddBranch( modifiedBranch.Select( a => new CloudNode( a ) ) );
			}

			// Early out when no new activity was added.
			if ( newTree == null )
			{
				return;
			}

			// Traverse whole new part of branch.
			Tree<CloudNode> currentLeaf = newTree;
			while ( currentLeaf != null )
			{
				// Manage activity.
				Activity curActivity = currentLeaf.Value.Activity;
				_activityNodes[ curActivity ] = currentLeaf;
				curActivity.AccessAddedEvent += ActivityAccessChangedEvent;
				curActivity.AccessRemovedEvent += ActivityAccessChangedEvent;

				// Add peers where necessary (when activity has more users which can access it than a peer higher up the tree).
				UpdatePeers( currentLeaf );

				currentLeaf = currentLeaf.Children.FirstOrDefault();
			}
		}

		/// <summary>
		///   Called when an activity no longer needs to be managed. Either because it was removed, or there is no desire to listen to activity cloud events.
		/// </summary>
		/// <param name="activity">The activity to stop managing.</param>
		public void UnmanageActivity( Activity activity )
		{
			// Retrieve node.
			Tree<CloudNode> node;
			_activityNodes.TryGetValue( activity, out node );
			if ( node == null )
			{
				return;
			}

			// Remove node from tree and stop managing all subactivities.
			UnmanageActivity( node );
			node.Remove();
		}

		void UnmanageActivity( Tree<CloudNode> node )
		{
			Activity activity = node.Value.Activity;
			_activityNodes.Remove( activity );
			if ( node.Parent == null )
			{
				_activityPeers.Remove( node ); // Remove root when root node.
			}
			activity.AccessAddedEvent -= ActivityAccessChangedEvent;
			activity.AccessRemovedEvent -= ActivityAccessChangedEvent;

			// TODO: Dispose 'node.Peers' when this becomes disposable? Should be garbage collected already.
            node.Value.Peers.Dispose();

			foreach ( var child in node.Children )
			{
				UnmanageActivity( child );
			}
		}

		void ActivityAccessChangedEvent( Activity activity, User user )
		{
			UpdatePeers( _activityNodes[ activity ] );
		}

		void UpdatePeers( Tree<CloudNode> node )
		{
			Activity curActivity = node.Value.Activity;
			int accessUsers = curActivity.AccessUsers.Count;

			// Update peer for current node.
			if ( accessUsers <= 1)
			{
				// Peer only needs to be added when more than the current user can access it.
			    if ( node.Value.Peers != null )
			    {
			        node.Value.Peers.Dispose();
			        node.Value.Peers = null;
			    }
			}
			else
			{
				Tree<CloudNode> higherPeer = node.Parent;
				if ( higherPeer == null || accessUsers > higherPeer.Value.Activity.AccessUsers.Count )
				{
					if ( node.Value.Peers == null )
					{
                        node.Value.Peers = GetOrCreateActivityPeer(curActivity);
					}
				}
				else
				{
					node.Value.Peers = null;
				}	
			}

			// Verify whether nodes underneath still need peers, in case same user which had access to child before got added to parent.
			foreach ( var child in node.Children )
			{
				if ( child.Value.Peers != null && accessUsers == child.Value.Activity.AccessUsers.Count )
				{
					child.Value.Peers = null;
				}
			}
		}

        protected abstract IActivityPeer GetOrCreateActivityPeer(Activity activity);
	}
}
