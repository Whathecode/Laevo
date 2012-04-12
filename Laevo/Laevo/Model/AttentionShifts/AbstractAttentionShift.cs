using System;
using System.Runtime.Serialization;


namespace Laevo.Model.AttentionShifts
{
	/// <summary>
	///   Represents a shift of attention at a certain point in time towards a certain object.
	/// </summary>
	/// <author>Steven Jeuris</author>
	[DataContract]
	public class AbstractAttentionShift
	{
		/// <summary>
		///   The time when the user shifted his attention towards the object.
		/// </summary>
		[DataMember]
		public DateTime Time { get; private set; }


		/// <summary>
		///   Create a new object which represents a shift of attention at a certain point in time.
		/// </summary>
		/// <param name = "time">The time when the user shifted his attention towards the object.</param>
		protected AbstractAttentionShift( DateTime time )
		{
			Time = time;
		}

		/// <summary>
		///   Create a new object which represents a shift of attention at the current time.
		/// </summary>
		protected AbstractAttentionShift()
		{
			Time = DateTime.Now;
		}
	}
}
