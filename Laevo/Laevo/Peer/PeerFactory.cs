using System;
using System.Collections.Generic;
using Laevo.Model;
using Laevo.Peer.Mock;


namespace Laevo.Peer
{
	public class PeerFactory : IPeerFactory, IDisposable
	{
	    readonly UsersPeer _usersPeer = new UsersPeer();
        readonly Dictionary<Activity, ActivityPeer> _activityPeers = new Dictionary<Activity, ActivityPeer>();

	    public IUsersPeer GetUsersPeer()
		{
			return _usersPeer;
		}

		public IActivityPeer GetActivityPeer( Activity activity )
		{
            ActivityPeer peer;
			if ( !_activityPeers.TryGetValue( activity, out peer ) )
			{
				peer = new ActivityPeer();
				_activityPeers[ activity ] = peer;
			}

			return peer;
		}

	    public void Dispose()
	    {
	        _usersPeer.Dispose();

	        foreach ( var activityPeer in _activityPeers.Values )
	        {
	            activityPeer.Dispose();
	        }
	    }
	}
}
