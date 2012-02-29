using System;
using System.Collections.Generic;
using Whathecode.System;
using Whathecode.System.Extensions;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.ActivityOverview
{
	class IrregularInterval : IInterval
	{
		readonly DateTimePart _part;
		readonly Func<DateTime, DateTime> _addition;

		readonly TimeSpan _minimumInterval;
		public TimeSpan MinimumInterval
		{
			get { return _minimumInterval; }
		}


		public IrregularInterval( TimeSpan minimumInterval, DateTimePart part, Func<DateTime, DateTime> addition )
		{
			_minimumInterval = minimumInterval;
			_part = part;
			_addition = addition;
		}


		public IEnumerable<DateTime> GetPositions( Interval<DateTime> range )
		{
			DateTime current = range.Start.Round( _part );
			yield return current;

			DateTime last = range.End.Round( _part );
			while ( current != last )
			{
				current = _addition( current );
				yield return current;
			}
		}
	}
}
