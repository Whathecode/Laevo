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
	}
}
