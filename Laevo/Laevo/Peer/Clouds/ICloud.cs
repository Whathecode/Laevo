using System;
using Laevo.Model;
using Whathecode.System.Aspects;


namespace Laevo.Peer.Clouds
{
    public interface ICloud : IDisposable
    {
        User User { get; set; }
        [InitializeEventHandlers(AttributeExclude = true)]
        event Action<User> PeerJoined;
        [InitializeEventHandlers(AttributeExclude = true)]
        event Action<User> PeerLeft;
        void Start( string cloudname );
    }
}