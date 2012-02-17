using System;
using System.Collections.Generic;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;


namespace Laevo.View.ActivityOverview
{
	class RegularLabels : AbstractTimeSpanLabels
	{
		readonly TimeSpan _interval;
		readonly Extensions.DateTimePart _rounding;


		public RegularLabels( TimeSpan interval, Extensions.DateTimePart rounding )
		{
			_interval = interval;
			_rounding = rounding;
		}


		protected override IEnumerable<DateTime> GetPositions( Interval<DateTime> interval )
		{
			DateTime current = interval.Start.Round( _rounding );
			while ( current <= interval.End )
			{
				yield return current;
				current += _interval;
			}
		}

		protected override TimeSpan GetMinimumTimeSpan()
		{
			return _interval;
		}
	}
}
