using Laevo.Model;


namespace Laevo.Peer.Mock
{
	class MockActivityPeer : IActivityPeer
	{
	    public User User { get; set; }
	    public string Cloudname { get; set; }
	    public void Start()
	    {
	        throw new System.NotImplementedException();
	    }
	}
}
