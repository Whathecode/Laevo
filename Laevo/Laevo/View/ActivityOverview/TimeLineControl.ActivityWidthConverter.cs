using System;
using System.Globalization;
using System.Windows.Data;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.ActivityOverview
{
	public partial class TimeLineControl
	{
		/// <summary>
		///   Determines the necessary width for an element on the time line.
		/// </summary>
		class ActivityWidthConverter : IMultiValueConverter
		{
			public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
			{
				var timeLineWidth = (double)values[ 0 ];
				var interval = (TimeInterval)values[ 1 ];
				
				var timeSpan = (TimeSpan)values[ 2 ];
				if ( timeSpan != TimeSpan.Zero )
				{
					return ((double)timeSpan.Ticks / interval.Size.Ticks) * timeLineWidth;
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
