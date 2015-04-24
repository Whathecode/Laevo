using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Laevo.Model;

namespace Laevo.Peer.Clouds
{
    [ServiceContract]
    public interface IUserCloud
    {
        [OperationContract(IsOneWay = true)]
        void Heartbeat(Peer status);

        [OperationContract(IsOneWay = true)]
        void Invite(User user, Activity activity);

    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class UserCloud : IUserCloud
    {
        #region Private fields
        private readonly User _me;
        private readonly Timer _heartbeat;
        private readonly ServiceHost _host;
        private readonly ChannelFactory<IUserCloud> _factory;
        private PeerState _state = PeerState.Online;
        private readonly Dictionary<User, Peer> _peers = new Dictionary<User, Peer>();
        private readonly int _frequency;
        #endregion

        #region Public fields
        public readonly IUserCloud Channel;
        #endregion

        #region Public events
        public event Action<Peer> PeerJoined;
        public event Action<Peer> PeerLeft;
        public event Action<Activity> InviteRecieved;
        public event Action<User, Activity> InviteResponse;
        #endregion

        #region Constructors
        public UserCloud(User me, int frequency = 10)
        {
            _me = me;
            _host = new ServiceHost(this);
            _host.Open();

            _factory = new ChannelFactory<IUserCloud>("IUserCloud");
            Channel = _factory.CreateChannel();

            _frequency = frequency;
            _heartbeat = new Timer(SendHeatbeat, null, new TimeSpan(0), new TimeSpan(0, 0, frequency));
        }
        #endregion

        #region Public methods
        public void Heartbeat(Peer peer)
        {

            if (_peers.ContainsKey(peer.User)) _peers.Remove(peer.User);
            else if (PeerJoined != null) PeerJoined(peer);

            if (peer.State == PeerState.Offline)
            {
                if (PeerLeft != null) PeerLeft(peer);
            }
            else
            {
                peer.LastHeartbeat = DateTime.Now;
                _peers.Add(peer.User, peer);
            }

            foreach (var p in _peers.Values.ToList().Where(p => DateTime.Now.Subtract(p.LastHeartbeat).TotalSeconds > _frequency * 3))
            {
                _peers.Remove(p.User);
                if (PeerLeft != null) PeerLeft(p);
            }

        }

        public void Invite(User user, Activity activity)
        {
            if (user.Equals(_me) && InviteRecieved != null)
                InviteRecieved(activity);
        }

        public void Stop()
        {
            _state = PeerState.Offline;
            _heartbeat.Dispose();
            SendHeatbeat(null);
            _factory.Close();
            _host.Close();
        }
        #endregion

        #region Private mehtods
        private void SendHeatbeat(object context)
        {
            var status = new Peer { User = _me, State = _state };
            Channel.Heartbeat(status);
        }
        #endregion
    }
}
