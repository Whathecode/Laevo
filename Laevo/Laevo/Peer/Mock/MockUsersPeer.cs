using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Laevo.Model;


namespace Laevo.Peer.Mock
{
	class MockUsersPeer : IUsersPeer
	{
		public event Action<Activity> Invited;


		public async Task<List<User>> GetUsers( string searchTerm )
		{
			// Fake time taken.
			var wait = Task.Delay( TimeSpan.FromSeconds( 2 ) );
			await wait;

			// Return dummy results.
			var result = new List<User>
			{
				new User { Name = "Foo " + searchTerm },
				new User { Name = "Bar " + searchTerm }
			};
			return result;
		}

		public void Invite( User user, Activity activity )
		{
			// Nothing to mock.
		}
	}
}
