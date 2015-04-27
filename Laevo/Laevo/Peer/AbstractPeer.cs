using System;
using System.Collections.Generic;
using Laevo.Model;
using Laevo.Peer.Clouds;
using Laevo.Peer.Mock;


namespace Laevo.Peer
{
    public abstract class AbstractPeer<T> : IPeer, IDisposable
        where T : class, ICloud, new()
    {
        public User User { get; set; }
        public string Cloudname { get; set; }

        protected readonly T Cloud;
        protected readonly List<User> Users = new List<User>();

        protected AbstractPeer()
        {
            Cloud = new T();
            Cloud.PeerJoined += peer => Users.Add(peer);
            Cloud.PeerLeft += peer => Users.Remove(peer);
        }

        public void Start()
        {
            if(User == null) throw new InvalidOperationException("User must be set");
            if(Cloudname.Trim() == "") throw new InvalidOperationException("Cloudname must be set");
            Cloud.User = User;
            Cloud.Start( Cloudname );
        }

        public void Dispose()
        {
            Cloud.Dispose();
        }

    }
}
