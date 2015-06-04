using System;
using System.ServiceModel;
using Laevo.Model;


namespace Laevo.Peer.Clouds.UserCloud
{
    [ServiceContract]
    public interface IUserCloud
    {
        [OperationContract(IsOneWay = true)]
        void Invite(User user, Activity activity);
    }

    [ServiceBehavior( InstanceContextMode = InstanceContextMode.Single )]
    public class UserCloud : Cloud<IUserCloud>, IUserCloud
    {
        public event Action<Activity> InviteRecieved;

        public UserCloud()
        {
            _proxy = new UserChannelProxy();
        }

        public void Invite( User user, Activity activity )
        {
            if ( user.Name == User.Name && activity != null && InviteRecieved != null )
                InviteRecieved(activity);    
        }
    }
}
