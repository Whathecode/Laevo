using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Whathecode.System.Arithmetic.Range;
using System.Windows;


namespace Laevo.View.ActivityOverview
{
	public partial class TimeLineControl
	{
		/// <summary>
		///   Determines the necessary offset for an element on the time line.
		/// </summary>
		class ActivityPositionConverter : IMultiValueConverter
		{
			public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
			{				
				TimeLineControl control = (TimeLineControl)parameter;

				var width = (double)values[ 0 ];
				var elementWidth = (double)values[ 1 ];
				var interval = (Interval<long>)values[ 2 ];

				object occurance = values[ 3 ];
				long occuranceTicks = 0;
				if ( occurance is DateTime )
				{
					occuranceTicks = ((DateTime)occurance).Ticks;
				}
				var alignment = (HorizontalAlignment)values[ 4 ];

				double percentage = interval.GetPercentageFor( occuranceTicks );
				double position = percentage * width;

				switch ( alignment )
				{
					case HorizontalAlignment.Left:
						return position;
					case HorizontalAlignment.Center:
						return position - (elementWidth / 2);
					case HorizontalAlignment.Right:
						return position - elementWidth;
					default:
						throw new NotSupportedException( alignment + " is not supported by the TimeLineControl." );
				}				
			}

			public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
			{
				throw new NotSupportedException();
			}
		}
	}
}
