using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Laevo.Model;


namespace Laevo.Peer
{
	public interface IActivityPeer : IDisposable
	{
	    void BroadcastActivity( Activity activity );
        event Action<Activity> RecievedActivity;
	}
}
