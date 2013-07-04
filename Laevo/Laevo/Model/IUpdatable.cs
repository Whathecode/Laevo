using System;


namespace Laevo.Model
{
	public interface IUpdatable
	{
		/// <summary>
		///   Updates the state of the object to the required state at the passed time.
		/// </summary>
		/// <param name = "now">The current time for which the object needs to be updated.</param>
		void Update( DateTime now );
	}
}
