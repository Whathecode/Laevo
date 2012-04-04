using System;
using System.Globalization;
using System.Windows.Data;
using Laevo.View.ActivityOverview;


namespace Laevo.View.Activity.Converters
{
	public class ActivityHeightConverter : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			double heightPercentage = (double)values[ 0 ];
			double headerHeight = (double)values[ 1 ];
			double availableHeight = (double)values[ 2 ] - ActivityOverviewWindow.TopOffset - ActivityOverviewWindow.BottomOffset;

			double size = (availableHeight * heightPercentage) - headerHeight;
			return size < 0 ? 0 : size;
		}

		public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
