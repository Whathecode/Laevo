using System;
using System.Globalization;
using System.Windows.Data;


namespace Laevo.View.ActivityOverview.Converters
{
	public class TimeLineWidthConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
		{
			// TODO: Calculate exactly how much bigger the time line should be to prevent excess rendering.
			return (double)value * 2;
		}

		public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
