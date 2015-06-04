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
        readonly IModelRepository _repository;

        public ActivityPeer( Activity activity )
        {
            _activity = activity;
            _repository = ServiceLocator.GetInstance().GetService<IModelRepository>();
            Cloudname = activity.Identifier.ToString();

            Cloud.ActivityRecieved += ActivityChanged;
            Cloud.ActivityRequested += SendActivity;
            Cloud.SyncRequested += SendStateTable;
            Cloud.StateTableRecieved += Merge;
        }

        void SendActivity(Guid id, Guid reciever, Guid sender)
        {
            var act = _repository.GetActivities( _activity ).SingleOrDefault( a => a.Identifier == id );
            if ( act != null ) Cloud.Proxy.SendActivity( act, User.Identifier, reciever );
        }

        void SendStateTable()
        {
            Cloud.Proxy.SendStateTable( ProduceStateTable(), User.Identifier );
        }

        Dictionary<Guid, DateTime> ProduceStateTable()
        {
            var states = _repository.GetActivities( _activity ).ToDictionary( activity => activity.Identifier, activity => activity.LastUpdated );
            states.Add( _activity.Identifier, _activity.LastUpdated );
            return states;
        }

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

        void ActivityChanged( Activity other )
        {
            var act = _repository.GetActivities(_activity).SingleOrDefault(a => a.Identifier == other.Identifier);

            if ( act == null )
            {
                ServiceLocator.GetInstance().GetService<Model.Laevo>().AddActivity( other );
            }
            else if ( act.LastUpdated < other.LastUpdated )
            {
                act.Merge( other );
            }
        }
    }
}
