using System.Collections.Generic;
using Laevo.Model;


namespace Laevo.Peer.Mock
{
	public class MockPeerFactory : IPeerFactory
	{
		readonly IUsersPeer _usersPeer = new MockUsersPeer();
		readonly Dictionary<Activity, IActivityPeer> _activityPeers = new Dictionary<Activity, IActivityPeer>();


		public IUsersPeer GetUsersPeer()
		{
			return _usersPeer;
		}

		public IActivityPeer GetActivityPeer( Activity activity )
		{
			IActivityPeer peer;
			if ( !_activityPeers.TryGetValue( activity, out peer ) )
			{
				peer = new MockActivityPeer();
				_activityPeers[ activity ] = peer;
			}

			return peer;
		}
	}
}
