using System;
using System.Collections.Generic;
using Laevo.Model;
using Laevo.Peer.Clouds;
using Laevo.Peer.Mock;


namespace Laevo.Peer
{
    public abstract class AbstractPeer<T> : IDisposable
        where T : class, ICloud, new()
    {
        public User User
        {
            get { return _user; }
            set
            {
                _user = value;
                if(!_started) Start();
            }
        }

        public string Cloudname { get; set; }

        protected readonly T Cloud;
        protected readonly List<User> Users = new List<User>();

        bool _started;
        User _user;


        protected AbstractPeer()
        {
            Cloud = new T();
            Cloud.PeerJoined += peer => Users.Add(peer);
            Cloud.PeerLeft += peer => Users.Remove(peer);
        }

        void Start()
        {
            if(User == null) throw new InvalidOperationException("User must be set");
            if(Cloudname.Trim() == "") throw new InvalidOperationException("Cloudname must be set");
            Cloud.User = User;
            Cloud.Start( Cloudname );
            _started = true;
        }

        public void Dispose()
        {
            Cloud.Dispose();
        }

    }
}
