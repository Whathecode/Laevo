using System;
using System.Collections.Generic;
using Whathecode.System;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;


namespace Laevo.View.ActivityOverview
{
	class RegularInterval
	{
		public Func<DateTime, DateTime> RoundToStart { get; private set; }
		public TimeSpan Interval { get; private set; }


		public RegularInterval( double every, DateTimePart step )
		{
			Func<double, TimeSpan> fromUnit = TimeSpanHelper.GetTimeSpanConstructor( step );
			RoundToStart = d => d.Round( step ) - fromUnit( d.GetDateTimePart( step ) % every );
			Interval = fromUnit( every );
		}

		public RegularInterval( Func<DateTime, DateTime> roundToStart, TimeSpan interval )
		{
			RoundToStart = roundToStart;
			Interval = interval;
		}


		/// <summary>
		///   Returns all the visible positions within a certain interval.
		/// </summary>
		public IEnumerable<DateTime> GetPositions( Interval<DateTime> range )
		{
			DateTime current = RoundToStart( range.Start );
			while ( current <= range.End )
			{
				yield return current;
				current += Interval;
			}			
		}
	}
}
