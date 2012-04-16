using System;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes.Coercion;


namespace Laevo.View.ActivityOverview
{
	public class VisibleIntervalCoercion : IControlCoercion<TimeLineControl.Properties, Interval<DateTime>>
	{
		public TimeLineControl.Properties DependentProperties
		{
			get
			{
				return
					TimeLineControl.Properties.Minimum | TimeLineControl.Properties.Maximum |
					TimeLineControl.Properties.MinimumTimeSpan | TimeLineControl.Properties.MaximumTimeSpan;
			}
		}

		public Interval<DateTime> Coerce( object context, Interval<DateTime> value )
		{
			var timeLine = (TimeLineControl)context;

			// Limit visible time span.
			TimeSpan desiredTimeSpan = value.End - value.Start;
			TimeSpan minTimeSpan = timeLine.MinimumTimeSpan ?? TimeSpan.FromHours( 0.5 );
			TimeSpan maxTimeSpan = timeLine.MaximumTimeSpan ?? TimeSpan.FromDays( 1000 );
			if ( desiredTimeSpan > maxTimeSpan || desiredTimeSpan < minTimeSpan )
			{
				return timeLine.VisibleInterval;
			}

			// Limit how far the time line goes.
			DateTime min = timeLine.Minimum ?? DateTime.MinValue;
			DateTime max = timeLine.Maximum ?? DateTime.MaxValue;
			var ticksInterval = new Interval<long>( value.Start.Ticks, value.End.Ticks );
			var limitedRange = new Interval<long>( min.Ticks, max.Ticks );
			var clamped = ticksInterval.Clamp( limitedRange );
			return new Interval<DateTime>( new DateTime( clamped.Start ), new DateTime( clamped.End ) );
		}
	}
}
