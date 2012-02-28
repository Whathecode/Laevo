using System;
using System.Collections.Generic;
using System.Linq;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.ActivityOverview.Labels
{
	class RegularIntervalLines : AbstractTimeSpanLabels
	{
		readonly RegularInterval _interval;
		readonly Func<DateTime, bool> _predicate;


		public RegularIntervalLines( TimeLineControl timeLine, RegularInterval interval, Func<DateTime, bool> predicate )
			: base( timeLine )
		{
			_interval = interval;
			_predicate = predicate;
		}


		protected override IEnumerable<DateTime> GetPositions( Interval<DateTime> interval )
		{
			return _interval.GetPositions( interval ).Where( d => _predicate( d ) );
		}

		protected override TimeSpan GetMinimumTimeSpan()
		{
			return _interval.MinimumInterval;
		}
	}
}
