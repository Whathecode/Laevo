using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.PeerToPeer;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Laevo.Model;
using Laevo.Peer.Clouds.PNRP;


namespace Laevo.Peer.Clouds
{
    public abstract class Cloud<T> : ICloud
    {
        #region Private fields
        private ServiceHost _host;
        private ChannelFactory<T> _factory;
        private Pnrp _pnrp;
        private Timer _heartbeat;
        private readonly HashSet<User> _peers = new HashSet<User>();
        #endregion

        #region Public events
        public event Action<User> PeerJoined;
        public event Action<User> PeerLeft;
        #endregion

        #region Public properties
        protected IProxy<T> _proxy;
        public T Proxy
        {
            get { return (T)_proxy; }
        }

        protected User User;

        #endregion

        #region Public Methods

        public void Start(string cloudname, User user)
        {
            User = user;
            _pnrp = new Pnrp(cloudname + "-laevo");
            var data = UserToByteArray(user);
            var port = _pnrp.Register(user.Identifier.ToString(), data);

            //Setting up host and channel factory
            var binding = new NetTcpBinding(SecurityMode.None);
            _host = new ServiceHost(this,
                new Uri(String.Format("net.tcp://[{0}]:{1}/ActivityCloud", IPAddress.IPv6Any, port)));
            _host.AddServiceEndpoint(typeof(T), binding, "");

            _host.Open();
            _factory = new ChannelFactory<T>( binding );

            _heartbeat = new Timer(SearchForPeers, null, new TimeSpan(0), new TimeSpan(0, 0, 10));
        }

        /// <summary>
        /// Returns a task that stops the cloud
        /// </summary>
        /// <returns>A task</returns>
        private Task Stop()
        {
            return new Task(() =>
            {
                try
                {
                    _heartbeat.Dispose();
                    _host.Close();
                    _pnrp.Dispose();
                    _factory.Close();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            });
        }

        /// <summary>
        /// Runs stop synchronously
        /// </summary>
        public void Dispose()
        {
            Stop().RunSynchronously();
        }
        #endregion

        #region Private methods

        /// <summary>
        /// Starting PNRP search for peers
        /// </summary>
        void SearchForPeers(object context)
        {
            var peers = new HashSet<User>();
            foreach ( var peer in _pnrp.Resolve() )
            {
                AddChannel( peer );

                var user = ByteArrayToUser( peer.Data );
                if (!_peers.Contains(user))
                {
                    _peers.Add(user);
                    PeerJoined(user);
                }
                peers.Add(user);
            }

            foreach ( var peer in _peers.Except( peers ).ToList() )
            {
                _peers.Remove( peer );
                PeerLeft( peer );
            }
        }

        /// <summary>
        /// Add channel to the proxy
        /// </summary>
        void AddChannel( PeerNameRecord pnr )
        {
            var id = Guid.Parse( pnr.Comment );
            if ( !_proxy.Contains( id ) )
            {
                foreach (var ep in pnr.EndPointCollection.Select(endpoint => new EndpointAddress("net.tcp://" + endpoint + "/ActivityCloud")))
                {
                    try
                    {
                        var channel = _factory.CreateChannel(ep);
                        _proxy.AddChannel(id, channel);
                        break;
                    }
                    catch
                    {
                        Debug.WriteLine( "A channel to {0} was not established", ep);
                    }
                }
            }
        }

        /// <summary>
        /// Serializer a user to a byte array
        /// </summary>
        /// <param name="user">The user to serialize</param>
        /// <returns>A byte array</returns>
        private static byte[] UserToByteArray(User user)
        {
            var serializer = new DataContractSerializer(typeof(User));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, user);
                return ms.ToArray();
            }
        }
        
        /// <summary>
        /// Deserializes a byte array to a user object
        /// </summary>
        /// <param name="data">The data to deserialize</param>
        /// <returns>A user</returns>
        private static User ByteArrayToUser( byte[] data )
        {
            var serializer = new DataContractSerializer( typeof( User ) );
            var input = new MemoryStream( data );
            var user = serializer.ReadObject( input );
            return user as User;
        }

        #endregion
    }
}
