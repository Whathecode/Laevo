using System;
using System.Runtime.Serialization;
using Laevo.Model;
using Whathecode.System.Aspects;


namespace Laevo.Peer.Clouds
{
    public interface ICloud : IDisposable
    {
        [InitializeEventHandlers(AttributeExclude = true)]
        event Action<User> PeerJoined;
        [InitializeEventHandlers(AttributeExclude = true)]
        event Action<User> PeerLeft;
        void Start( string cloudname, User user );
    }
}