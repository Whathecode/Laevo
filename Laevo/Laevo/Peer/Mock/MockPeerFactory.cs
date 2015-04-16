namespace Laevo.Peer.Mock
{
	public class MockPeerFactory : IPeerFactory
	{
		public IUsersPeer GetUsersPeer()
		{
			return new MockUsersPeer();
		}
	}
}
