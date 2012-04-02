using System;
using System.Globalization;
using System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class ActivityHeightConverter : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			// TODO: Get these offsets from somewhere else instead.
			const double topOffset = 105;
			const double bottomOffset = 45;

			double heightPercentage = (double)values[ 0 ];
			double headerHeight = (double)values[ 1 ];
			double availableHeight = (double)values[ 2 ] - topOffset - bottomOffset;

			double size = (availableHeight * heightPercentage) - headerHeight;
			return size < 0 ? 0 : size;
		}

		public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
