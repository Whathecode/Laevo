using Laevo.Model;


namespace Laevo.Peer
{
	public interface IPeerFactory
	{
		IUsersPeer GetUsersPeer();
		IActivityPeer GetActivityPeer( Activity activity );
	}
}
