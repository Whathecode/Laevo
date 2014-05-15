using System;
using System.Runtime.Serialization;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.Model
{
	/// <summary>
	///   An interval which indicates when an activity was planned in time.
	/// </summary>
	[DataContract]
	public class PlannedInterval
	{
		/// <summary>
		///   The interval when the activity is planned.
		/// </summary>
		[DataMember]
		public Interval<DateTime> Interval { get; set; }

		/// <summary>
		///   Indicates the moment in time the planned interval was created.
		/// </summary>
		[DataMember]
		public DateTime PlannedAt { get; private set; }


		public PlannedInterval( DateTime start, DateTime end )
		{
			Interval = new Interval<DateTime>( start, end );
			PlannedAt = DateTime.Now;
		}
	}
}
