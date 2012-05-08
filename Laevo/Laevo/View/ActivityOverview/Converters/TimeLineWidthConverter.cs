using System;
using System.Globalization;
using System.Windows.Data;


namespace Laevo.View.ActivityOverview.Converters
{
	public class TimeLineWidthConverter : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			double width = (double)values[ 0 ];
			float renderSize = (float)values[ 1 ];

			// TODO: Calculate exactly how much bigger the time line should be to prevent excess rendering.
			return (width * renderSize) * (6/4.0);
		}

		public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
