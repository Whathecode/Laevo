using Laevo.Model;


namespace Laevo.Peer.Mock
{
	public class MockPeerFactory : AbstractPeerFactory
	{
		readonly IUsersPeer _usersPeer = new MockUsersPeer();
		public override IUsersPeer UsersPeer
		{
			get { return _usersPeer; }
		}


		protected override IActivityPeer GetOrCreateActivityPeer( Activity activity )
		{
			return new MockActivityPeer();
		}
	}
}
