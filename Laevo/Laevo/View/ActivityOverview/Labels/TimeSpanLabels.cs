using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.ActivityOverview.Labels
{
	/// <summary>
	///   Manages a set of labels indicating time intervals.
	/// </summary>
	/// <author>Steven Jeuris</author>
	class TimeSpanLabels : AbstractLabels<Line>
	{
		const double MinimumSpaceBetweenLabels = 20.0;

		readonly IInterval _interval;
		readonly TimeSpan _minimumInterval;
		readonly Func<DateTime, bool> _predicate;


		public TimeSpanLabels(
			TimeLineControl timeLine,
			IInterval interval,
			TimeSpan minimumInterval,
			Func<DateTime, bool> predicate )
			: base( timeLine )
		{
			_interval = interval;
			_minimumInterval = minimumInterval;
			_predicate = predicate;
		}


		protected override IEnumerable<DateTime> GetPositions( Interval<DateTime> interval )
		{
			return _interval.GetPositions( interval ).Where( d => _predicate( d ) );
		}

		protected override bool ShouldShowLabels()
		{
			long minimumTicks = _minimumInterval.Ticks;
			int maximumLabels = (int)Math.Ceiling( TimeLine.ActualWidth / MinimumSpaceBetweenLabels );
			return TimeLine.GetVisibleTicks() / minimumTicks < maximumLabels;
		}

		protected override bool IsVisible( Line label, DateTime occurance )
		{
			return TimeLine.VisibleInterval.LiesInInterval( occurance );
		}

		protected override Line CreateNewLabel()
		{
			return new Line
			{
				X1 = 0,
				Y1 = 0,
				X2 = 0,
				Y2 = 500,
				Stroke = Brushes.White,
				StrokeThickness = 1,
				HorizontalAlignment = HorizontalAlignment.Center
			};
		}

		protected override void UpdateLabel( Line label )
		{
			long minimumTicks = _minimumInterval.Ticks;
			double minimumWidth = (double)minimumTicks / TimeLine.GetVisibleTicks() * TimeLine.ActualWidth;
			const double maxHeight = 2000;  // TODO: Limit to maximum screen height.
			label.Y2 = minimumWidth > maxHeight ? maxHeight : minimumWidth;
		}
	}
}
