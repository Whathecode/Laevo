using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.ActivityOverview.Labels
{
	abstract class AbstractIntervalLabels<T> : AbstractLabels<T>
		where T : FrameworkElement
	{
		protected const double MinimumSpaceBetweenLabels = 100.0;

		protected readonly IInterval Interval;
		readonly Func<DateTime, bool> _predicate;


		protected AbstractIntervalLabels(
			TimeLineControl timeLine,
			IInterval interval,
			Func<DateTime, bool> predicate,
			TimeSpan extendVisibleRange )
			: base( timeLine, extendVisibleRange )
		{
			Interval = interval;
			_predicate = predicate;
		}


		protected override IEnumerable<DateTime> GetPositions( Interval<DateTime> interval )
		{
			return Interval.GetPositions( interval ).Where( d => _predicate( d ) );
		}

		protected override bool IsVisible( T label, DateTime occurance )
		{
			return ExtendedVisibleRange.LiesInInterval( occurance );
		}

		public bool LabelsFitScreen()
		{
			long minimumTicks = Interval.MinimumInterval.Ticks;
			int maximumLabels = (int)Math.Ceiling( TimeLine.ActualWidth / MinimumSpaceBetweenLabels );
			return TimeLine.GetVisibleTicks() / minimumTicks < maximumLabels;
		}
	}
}
