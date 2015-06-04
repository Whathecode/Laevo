using System;
using System.Collections.Generic;
using Laevo.Model;
using Laevo.Peer.Clouds;

namespace Laevo.Peer
{
    public abstract class AbstractPeer<T> : IDisposable
        where T : class, ICloud, new()
    {
        protected string Cloudname { private get; set; }

        protected readonly T Cloud;
        protected readonly List<User> Users = new List<User>();

        bool _started;
        protected User User;


        protected AbstractPeer()
        {
            Cloud = new T();
            Cloud.PeerJoined += peer => Users.Add(peer);
            Cloud.PeerLeft += peer => Users.Remove(peer);
        }

        public void Start(User user)
        {
            if (_started) throw new InvalidOperationException( "Cloud is already started" );
            if ((User = user) == null) throw new InvalidOperationException("User must be set");
            if(Cloudname.Trim() == "") throw new InvalidOperationException("Cloudname must be set");
            Cloud.Start(Cloudname, user);
            _started = true;
        }

        public void Dispose()
        {
            Cloud.Dispose();
        }

    }
}
