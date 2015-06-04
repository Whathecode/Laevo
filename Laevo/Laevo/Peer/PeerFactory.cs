using System;
using System.Collections.Generic;
using Laevo.Data.Model;
using Laevo.Model;


namespace Laevo.Peer
{
	public class PeerFactory : AbstractPeerFactory, IDisposable
	{
	    readonly UsersPeer _usersPeer;
        readonly Dictionary<Activity, ActivityPeer> _activityPeers = new Dictionary<Activity, ActivityPeer>();

	    public PeerFactory()
	    {
            _usersPeer = new UsersPeer( );
	    }

	    public override IUsersPeer UsersPeer
	    {
	        get { return _usersPeer; }
	    }

	    protected override IActivityPeer GetOrCreateActivityPeer( Activity activity )
	    {
            ActivityPeer peer;
            if (!_activityPeers.TryGetValue(activity, out peer))
            {
                peer = new ActivityPeer(activity);
                _activityPeers[activity] = peer;
                peer.Start( ServiceLocator.GetInstance().GetService<IModelRepository>().User );
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
