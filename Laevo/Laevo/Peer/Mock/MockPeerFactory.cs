using Laevo.Model;


namespace Laevo.Peer.Mock
{
	public class MockPeerFactory : IPeerFactory
	{
		public IUsersPeer GetUsersPeer()
		{
			return new MockUsersPeer();
		}

		public IActivityPeer GetActivityPeer( Activity activity )
		{
			return new MockActivityPeer();
		}
	}
}
