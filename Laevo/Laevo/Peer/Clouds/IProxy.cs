using System;
using System.Collections.Generic;


namespace Laevo.Peer.Clouds
{
    public interface IProxy<in T> {
        void AddChannel( Guid guid, T t );
        bool Contains( Guid guid );
    }
} 