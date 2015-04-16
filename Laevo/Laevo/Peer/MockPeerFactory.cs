using Laevo.Peer.Mock;


namespace Laevo.Peer
{
	public class MockPeerFactory : IPeerFactory
	{
		public IUsersPeer GetUsersPeer()
		{
			return new MockUsersPeer();
		}
	}
}
