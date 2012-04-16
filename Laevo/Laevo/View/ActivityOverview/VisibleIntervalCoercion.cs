using System;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes.Coercion;


namespace Laevo.View.ActivityOverview
{
	public class VisibleIntervalCoercion : IControlCoercion<TimeLineControl.Properties, Interval<DateTime>>
	{
		public TimeLineControl.Properties DependentProperties
		{
			get { return TimeLineControl.Properties.Minimum | TimeLineControl.Properties.Maximum; }
		}

		public Interval<DateTime> Coerce( object context, Interval<DateTime> value )
		{
			TimeLineControl timeLine = (TimeLineControl)context;
			DateTime min = timeLine.Minimum ?? DateTime.MinValue;
			DateTime max = timeLine.Maximum ?? DateTime.MaxValue;

			var ticksInterval = new Interval<long>( value.Start.Ticks, value.End.Ticks );
			var limitedRange = new Interval<long>( min.Ticks, max.Ticks );
			var clamped = ticksInterval.Clamp( limitedRange );
			return new Interval<DateTime>( new DateTime( clamped.Start ), new DateTime( clamped.End ) );
		}
	}
}
