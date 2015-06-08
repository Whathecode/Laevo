using System;
using System.Collections.Generic;
using System.Linq;
using Laevo.Data.Model;
using Laevo.Model;
using Laevo.Peer.Clouds.ActivityCloud;


namespace Laevo.Peer
{
    class ActivityPeer : AbstractPeer<ActivityCloud>, IActivityPeer
    {
        readonly Activity _activity;
        readonly IModelRepository _repo;

        public event Action<Activity> RecievedActivity;

        /// <summary>
        /// Constructs a ActivityPeer.
        /// </summary>
        /// <param name="activity">The parent activity</param>
        public ActivityPeer( Activity activity )
        {
            _activity = activity;
            Cloudname = activity.Identifier.ToString();
            _repo = ServiceLocator.GetInstance().GetService<IModelRepository>();
            Cloud.ActivityRecieved += RecievedActivity;
            Cloud.ActivityRequested += SendActivity;
            Cloud.SyncRequested += SendStateTable;
            Cloud.StateTableRecieved += Merge;
        }

        /// <summary>
        /// Broadcasts activity to all peers in group
        /// </summary>
        /// <param name="activity">The activity to broadcast</param>
        public void BroadcastActivity( Activity activity )
        {
            Cloud.Proxy.BroadcastActivity( activity );
        }

        /// <summary>
        /// Sends activity with given id to given reciever
        /// </summary>
        /// <param name="id">The id of the activity to send</param>
        /// <param name="reciever">The reciever of the activity</param>
        /// <param name="sender">The sender of the activity</param>
        void SendActivity(Guid id, Guid reciever, Guid sender)
        {
            var act = _repo.GetActivities( _activity ).SingleOrDefault(a => a.Identifier == id);
            if ( act != null ) Cloud.Proxy.SendActivity( act, User.Identifier, reciever );
        }

        /// <summary>
        /// Produces and broadcast statetable of all activies managed.
        /// </summary>
        void SendStateTable()
        {
            Cloud.Proxy.SendStateTable( ProduceStateTable(), User.Identifier );
        }

        /// <summary>
        /// Produces a state table that contains all activities managed and their last changed date.
        /// </summary>
        /// <returns>The state table</returns>
        Dictionary<Guid, DateTime> ProduceStateTable()
        {
            var states = _repo.GetActivities(_activity).ToDictionary(activity => activity.Identifier, activity => activity.LastUpdated);
            states.Add( _activity.Identifier, _activity.LastUpdated );
            return states;
        }

        /// <summary>
        /// Compares two state table and request any activities that is outdated or not pressent locally.
        /// </summary>
        /// <param name="other">The other peers state table</param>
        /// <param name="peer">The sender of the state table</param>
        void Merge( Dictionary<Guid, DateTime> other, Guid peer )
        {
            var table = ProduceStateTable();

            foreach ( var key in other.Intersect( table ).Select( kvp => kvp.Key ).Where( key => table[ key ] < other[ key ] ) )
            {
                Cloud.Proxy.RequestActivity( key, User.Identifier, peer );
            }

            foreach ( var key in other.Except( table ).Select( kvp => kvp.Key ).Where( other.ContainsKey ) )
            {
                Cloud.Proxy.RequestActivity( key, User.Identifier, peer );
            }
        }
    }
}
