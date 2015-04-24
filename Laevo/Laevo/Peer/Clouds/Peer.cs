using System;
using System.Runtime.Serialization;
using Laevo.Model;

namespace Laevo.Peer.Clouds
{
    [DataContract]
    public class Peer
    {
        [DataMember]
        public DateTime LastHeartbeat { get; set; }
        [DataMember]
        public User User { get; set; }
        [DataMember]
        public PeerState State { get; set; }

    }
}