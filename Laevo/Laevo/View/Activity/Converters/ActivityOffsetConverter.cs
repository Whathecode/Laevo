using System;
using System.Globalization;
using System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class ActivityOffsetConverter : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			// TODO: Get these offsets from somewhere else instead.
			const double topOffset = 90;
			const double bottomOffset = 45;

			double offsetPercentage = (double)values[ 0 ];
			double availableHeight = (double)values[ 1 ] - topOffset - bottomOffset;

			return (availableHeight * offsetPercentage) + bottomOffset;
		}

		public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
