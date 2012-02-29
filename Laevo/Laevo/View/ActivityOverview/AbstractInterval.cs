using System;
using System.Collections.Generic;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.ActivityOverview
{
	interface IInterval
	{
		/// <summary>
		///   The minimum possible interval represented by this interval.
		/// </summary>
		TimeSpan MinimumInterval { get; }

		/// <summary>
		///   Returns all the visible positions within a certain interval.
		/// </summary>
		IEnumerable<DateTime> GetPositions( Interval<DateTime> range );
	}
}
