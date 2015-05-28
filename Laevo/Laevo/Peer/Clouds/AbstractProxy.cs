using System;
using System.Collections.Generic;


namespace Laevo.Peer.Clouds
{
    public abstract class AbstractProxy<T> : IProxy<T>
    {
        public Dictionary<Guid, T> Channels { get; set; }

        protected AbstractProxy()
        {
            Channels = new Dictionary<Guid, T>();
        }
    }
}