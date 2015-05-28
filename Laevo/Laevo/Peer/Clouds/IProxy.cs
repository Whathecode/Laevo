using System;
using System.Collections.Generic;


namespace Laevo.Peer.Clouds
{
    public interface IProxy<T>
    {
        Dictionary<Guid, T> Channels { get; set; }
    }
}