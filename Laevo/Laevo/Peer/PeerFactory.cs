using System;
using System.Collections.Generic;
using Laevo.Model;


namespace Laevo.Peer
{
	public class PeerFactory : AbstractPeerFactory, IDisposable
	{
	    readonly UsersPeer _usersPeer = new UsersPeer();
        readonly Dictionary<Activity, ActivityPeer> _activityPeers = new Dictionary<Activity, ActivityPeer>();

	    public override IUsersPeer UsersPeer
	    {
	        get { return _usersPeer; }
	    }

	    protected override IActivityPeer CreateActivityPeer( Activity activity )
	    {
            ActivityPeer peer;
            if (!_activityPeers.TryGetValue(activity, out peer))
            {
                peer = new ActivityPeer();
                _activityPeers[activity] = peer;
            }

            return peer;
	    }

        public void Dispose()
        {
            _usersPeer.Dispose();

            foreach (var activityPeer in _activityPeers.Values)
            {
                activityPeer.Dispose();
            }
        }
	}
}
