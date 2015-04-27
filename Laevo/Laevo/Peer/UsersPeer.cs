using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Laevo.Model;
using Laevo.Peer.Clouds.UserCloud;
using Whathecode.System.Aspects;


namespace Laevo.Peer
{
    public class UsersPeer : AbstractPeer<UserCloud>, IUsersPeer
    {
        /// <summary>
        /// Constructs a new UsersPeer object.
        /// </summary>
        public UsersPeer()
        {
            Cloudname = "usercloud";
            Cloud.InviteRecieved += activity => Invited(activity);
        }

        /// <summary>
        /// Searches the local cache of online users
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        public Task<List<User>> GetUsers(string searchTerm)
        {
            return Task.Run(() => Users.Where(t => t.Name != null && t.Name.Contains(searchTerm)).ToList());
        }

        /// <summary>
        /// Invites the given user to the activity cloud
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="activity">The activity</param>
        public void Invite(User user, Activity activity)
        {
            Cloud.Proxy.Invite(user, activity);
        }

        [InitializeEventHandlers(AttributeExclude = true)] // TODO: Why doesn't this compile when aspect is not excluded here?
        public event Action<Activity> Invited;

    }
}
