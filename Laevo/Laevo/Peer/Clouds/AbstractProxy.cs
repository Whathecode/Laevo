using System;
using System.Collections.Generic;
using System.Linq;

namespace Laevo.Peer.Clouds
{
    public abstract class AbstractProxy<T> : IProxy<T>
    {
        private Dictionary<Guid, T> Channels { get; set; }   

        protected AbstractProxy()
        {
            Channels = new Dictionary<Guid, T>();
        }

        public void AddChannel( Guid guid, T t )
        {
            Channels.Add( guid, t );
        }

        protected void RemoveChannel( Guid guid )
        {
            Channels.Remove( guid );
        }

        protected IEnumerable<KeyValuePair<Guid, T>> GetChannels()
        {
            return Channels.ToList();
        }

        public bool Contains( Guid guid )
        {
            return Channels.ContainsKey( guid );
        }

        protected T GetChannel( Guid guid )
        {
            return Channels[ guid ];
        }

    }
}