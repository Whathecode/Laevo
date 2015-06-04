using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Laevo.Model;
using Whathecode.System.Aspects;


namespace Laevo.Peer
{
	public interface IUsersPeer : IDisposable
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

		/// <summary>
		///   Event which is raised when the current user is invited to access an activity.
		/// </summary>
		[InitializeEventHandlers( AttributeExclude = true )] // TODO: Why doesn't this compile when aspect is not excluded here?
		event Action<Activity> Invited;

	    void Start( User user );

	}
}
