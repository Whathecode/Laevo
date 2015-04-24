using System.Runtime.Serialization;

namespace Laevo.Peer.Clouds
{
    [DataContract]
    public enum PeerState
    {
        [EnumMember]
        Unknown,
        [EnumMember]
        Offline,
        [EnumMember]
        Online
    }
}