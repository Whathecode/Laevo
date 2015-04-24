using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Laevo.Model;
using Laevo.Peer.Clouds;

namespace Laevo.Peer
{
    class UsersPeer : IUsersPeer
    {
        private readonly UserCloud _userCloud;
        private readonly List<User> _users = new List<User>(); 

        /// <summary>
        /// Constructs a new UsersPeer object.
        /// </summary>
        /// <param name="me">The user logged in to the system</param>
        public UsersPeer(User me)
        {
            _userCloud = new UserCloud(me);
            _userCloud.InviteRecieved += activity => Invited(activity);
            _userCloud.PeerJoined += peer => _users.Add(peer.User);
            _userCloud.PeerLeft += peer => _users.Remove(peer.User);
        }

        /// <summary>
        /// Searches the local cache of online users
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        public Task<List<User>> GetUsers(string searchTerm)
        {
            return Task.Run(() => _users.Where(t => t.Name != null && t.Name.Contains(searchTerm)).ToList());
        }

        /// <summary>
        /// Invites the given user to the activity cloud
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="activity">The activity</param>
        public void Invite(User user, Activity activity)
        {
            _userCloud.Channel.Invite(user, activity);
        }

        public event Action<Activity> Invited;
    }
}
