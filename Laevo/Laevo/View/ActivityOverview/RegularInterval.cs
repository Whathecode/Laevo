using System;
using System.Collections.Generic;
using Whathecode.System;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;


namespace Laevo.View.ActivityOverview
{
	class RegularInterval : IInterval
	{	
		public Func<DateTime, DateTime> RoundToStart { get; private set; }

		readonly TimeSpan _minimumInterval;
		public TimeSpan MinimumInterval
		{
			get { return _minimumInterval; }
		}


		public RegularInterval( int every, DateTimePart step )
		{			
			Func<double, TimeSpan> fromUnit = TimeSpanHelper.GetTimeSpanConstructor( step );
			RoundToStart = d => d.Round( step ) - fromUnit( d.GetDateTimePart( step ) % every );
			_minimumInterval = fromUnit( every );
		}

		public RegularInterval( Func<DateTime, DateTime> roundToStart, TimeSpan interval )
		{
			RoundToStart = roundToStart;
			_minimumInterval = interval;
		}


		/// <summary>
		///   Returns all the visible positions within a certain interval.
		/// </summary>
		public IEnumerable<DateTime> GetPositions( TimeInterval range )
		{
			DateTime current = RoundToStart( range.Start );
			while ( current <= range.End && current.Ticks + MinimumInterval.Ticks < DateTime.MaxValue.Ticks )
			{
				yield return current;
				current += MinimumInterval;
			}			
		}
	}
}
