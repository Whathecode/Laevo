using System.Collections.Generic;
using System.Threading.Tasks;
using Laevo.Model;


namespace Laevo.Peer
{
	public interface IUsersPeer
	{
		/// <summary>
		///   Requests all endpoints in the mesh for their user information.
		/// </summary>
		Task<List<User>> GetUsers( string searchTerm );

		/// <summary>
		///   Invites a user to participate in a specified activity.
		/// </summary>
		/// <param name="user">The user to invite.</param>
		/// <param name="activity">The activity to which the user is invited.</param>
		void Invite( User user, Activity activity );
	}
}
