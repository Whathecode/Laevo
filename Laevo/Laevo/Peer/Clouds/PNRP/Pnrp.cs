using System;
using System.Net;
using System.Net.PeerToPeer;
using System.Net.Sockets;

namespace Laevo.Peer.Clouds.PNRP
{
    public class Pnrp : IDisposable
    {
        #region Private fields
        private readonly string _identifier;
        private readonly int _port;
        private PeerNameRegistration _pnReg;
        #endregion

        #region Constructors

        /// <summary>
        /// Constructs new PNRP module
        /// </summary>
        /// <param name="identifier">The cloud identifier</param>
        public Pnrp(string identifier) : this(identifier, FindFreePort())
        {

        }

        /// <summary>
        /// Constructs new PNRP module
        /// </summary>
        /// <param name="identifier">The cloud identifier</param>
        /// <param name="port">The port to use</param>
        public Pnrp(string identifier, int port)
        {
            _identifier = identifier;
            _port = port;
        }

        #endregion

        #region Public methods
        /// <summary>
        /// Registers on all available PNRP clouds 
        /// </summary>
        /// <param name="comment">The comment to attach to the registration</param>
        /// <returns></returns>
        public int Register(string comment)
        {
            var peerName = new PeerName(_identifier, PeerNameType.Unsecured);
            _pnReg = new PeerNameRegistration(peerName, _port) { Comment = comment, UseAutoEndPointSelection = true };
            _pnReg.Start();
            return _port;
        }

        /// <summary>
        /// Register on all available PNRP clouds
        /// </summary>
        /// <param name="comment">The comment to attach to the registration</param>
        /// <param name="data">The data to attach to the registration</param>
        /// <returns></returns>
        public int Register( string comment, byte[] data )
        {
            var peerName = new PeerName(_identifier, PeerNameType.Unsecured);
            _pnReg = new PeerNameRegistration( peerName, _port ) { Comment = comment, UseAutoEndPointSelection = true, Data = data };
            _pnReg.Start();
            return _port;
        }

        /// <summary>
        /// Resolvs available peers in the cloud
        /// </summary>
        /// <returns>Returns a collection of found peers</returns>
        public PeerNameRecordCollection Resolve()
        {
            var resolver = new PeerNameResolver();
            var peerName = new PeerName(_identifier, PeerNameType.Unsecured);
            return resolver.Resolve(peerName);
        }

        /// <summary>
        /// Disposes the element and closes the registration
        /// </summary>
        public void Dispose()
        {
            _pnReg.Stop();
        }

        #endregion

        #region Private static methods

        static int FindFreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
        #endregion
    }
}
