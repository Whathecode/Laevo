using System;
using System.Diagnostics;
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
		class TimeLinePositionConverter : IMultiValueConverter
		{
			public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
			{
				var width = (double)values[ 0 ];
				var elementWidth = (double)values[ 1 ];
				var interval = (Interval<DateTime>)values[ 2 ];
				var ticksInterval = new Interval<long>( interval.Start.Ticks, interval.End.Ticks );
				object occurance = values[ 3 ];
				long occuranceTicks = 0;
				if ( occurance is DateTime )
				{
					occuranceTicks = ((DateTime)occurance).Ticks;
				}

				double position = ticksInterval.GetPercentageFor( occuranceTicks ) * width;
				return position - (elementWidth / 2);
			}

			public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
			{
				throw new NotSupportedException();
			}
		}
	}
}
