using System;
using System.Collections.Generic;
using Whathecode.System;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;


namespace Laevo.View.ActivityOverview
{
	class RegularInterval
	{
		readonly string _formatString;

		public Func<DateTime, DateTime> RoundToStart { get; private set; }
		public TimeSpan Interval { get; private set; }


		public RegularInterval( double every, DateTimePart step, string formatString )
		{			
			Func<double, TimeSpan> fromUnit = TimeSpanHelper.GetTimeSpanConstructor( step );
			RoundToStart = d => d.Round( step ) - fromUnit( d.GetDateTimePart( step ) % every );
			Interval = fromUnit( every );
			_formatString = formatString;
		}

		public RegularInterval( Func<DateTime, DateTime> roundToStart, TimeSpan interval, string formatString )
		{
			RoundToStart = roundToStart;
			Interval = interval;
			_formatString = formatString;
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

		public string Format( DateTime occurance )
		{
			return occurance.ToString( _formatString );
		}
	}
}
