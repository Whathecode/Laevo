using System.ServiceModel;

namespace Laevo.Peer.Clouds.ActivityCloud
{
    [ServiceContract]
    public interface IActivityCloud
    {
        //[OperationContract(IsOneWay = true)]
    }

    [ServiceBehavior( InstanceContextMode = InstanceContextMode.Single )]
    class ActivityCloud : Cloud<IActivityCloud>, IActivityCloud
    {
        public ActivityCloud()
        {
            _proxy = new ActivityChannelProxy();
        }
    }
}
