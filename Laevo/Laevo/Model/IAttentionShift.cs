using System;


namespace Laevo.Model
{
	/// <summary>
	///   Represents a shift of attention at a certain point in time towards a certain object.
	/// </summary>
	/// <typeparam name = "T">The type of object towards which the attention shifted.</typeparam>
	/// <author>Steven Jeuris</author>
	public interface IAttentionShift<out T>
	{
		/// <summary>
		///   The time when the user shifted his attention towards the object.
		/// </summary>
		DateTime Time { get; }

		/// <summary>
		///   The object towards which attention was shifted.
		/// </summary>
		T Object { get; }
	}
}
