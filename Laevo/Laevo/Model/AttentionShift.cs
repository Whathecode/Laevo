using System;


namespace Laevo.Model
{
	/// <summary>
	///   Represents a shift of attention at a certain point in time towards a certain object.
	/// </summary>
	/// <typeparam name = "T">The type of object towards which the attention shifted.</typeparam>
	/// <author>Steven Jeuris</author>
	public class AttentionShift<T> : IAttentionShift<T>
	{
		/// <summary>
		///   The time when the user shifted his attention towards the object.
		/// </summary>
		public DateTime Time { get; private set; }

		/// <summary>
		///   The object towards which attention was shifted.
		/// </summary>
		public T Object { get; private set; }


		/// <summary>
		///   Create a new object which represents a shift of attention at a certain point in time.
		/// </summary>
		/// <param name = "time">The time when the user shifted his attention towards the object.</param>
		/// <param name = "object">The object towards which attention was shifted.</param>
		public AttentionShift( DateTime time, T @object )
		{
			Time = time;
			Object = @object;
		}
	}
}
