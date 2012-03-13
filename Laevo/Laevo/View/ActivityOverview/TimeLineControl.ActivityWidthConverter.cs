using System;
using System.Globalization;
using System.Windows.Data;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.ActivityOverview
{
	public partial class TimeLineControl
	{
		/// <summary>
		///   Determines the necessary offset for an element on the time line.
		/// </summary>
		class ActivityWidthConverter : IMultiValueConverter
		{
			public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
			{
				var width = (double)values[ 0 ];
				var elementWidth = (double)values[ 1 ];
				var interval = (Interval<DateTime>)values[ 2 ];
				var ticksInterval = new Interval<long>( interval.Start.Ticks, interval.End.Ticks );

				TimeSpan timeSpan = (TimeSpan)values[ 3 ];
				if ( timeSpan != TimeSpan.Zero )
				{
					return ((double)timeSpan.Ticks / ticksInterval.Size) * width;
				}
				return double.NaN;
			}

			public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
			{
				throw new NotSupportedException();
			}
		}
	}
}
