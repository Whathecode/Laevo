using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using Laevo.Model;


namespace Laevo.Peer.Clouds.ActivityCloud
{
    [ServiceContract]
    public interface IActivityCloud
    {
        [OperationContract(IsOneWay = true)]
        void RequestSync();

        [OperationContract( IsOneWay = true )]
        void SendStateTable( Dictionary<Guid, DateTime> activityStates, Guid sender);

        [OperationContract(IsOneWay = true)]
        void RequestActivity(Guid activityId, Guid sender, Guid reciever);

        [OperationContract(IsOneWay = true)]
        void SendActivity(Activity activity, Guid sender, Guid reciever);

        [OperationContract(IsOneWay = true)]
        void BroadcastActivity(Activity activity);
    }

    [ServiceBehavior( InstanceContextMode = InstanceContextMode.Single )]
    class ActivityCloud : Cloud<IActivityCloud>, IActivityCloud
    {

        public event Action<Activity> ActivityRecieved;
        public event Action<Guid, Guid, Guid> ActivityRequested;
        public event Action<Dictionary<Guid, DateTime>, Guid> StateTableRecieved;
        public event Action SyncRequested;

        readonly Timer _heartbeat;

        public ActivityCloud()
        {
            _proxy = new ActivityChannelProxy();
            _heartbeat = new Timer(Sync, null, new TimeSpan(60), new TimeSpan(0, 0, 60));
        }

        void Sync( object context )
        {
            Proxy.RequestSync();
        }

        public void RequestSync()
        {
            SyncRequested();
        }

        public void SendStateTable(Dictionary<Guid, DateTime> activityStates, Guid sender)
        {
            if ( activityStates.Count > 0 && sender != Guid.Empty )
                StateTableRecieved( activityStates, sender );
        }

        public void RequestActivity(Guid activityId, Guid sender, Guid reciever)
        {
            if ( activityId != Guid.Empty )
                ActivityRequested(activityId, sender, reciever);
        }

        public void SendActivity( Activity activity,  Guid sender, Guid reciever )
        {
            if (activity != null)
                ActivityRecieved(activity);
        }

        public void BroadcastActivity( Activity activity)
        {
            if (activity != null)
                ActivityRecieved(activity);
        }

        public new void Dispose()
        {
            _heartbeat.Dispose();
            base.Dispose();
        }
    }
}
