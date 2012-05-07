using System;
using System.Globalization;
using System.Windows.Data;


namespace Laevo.View.ActivityOverview.Converters
{
	public class TimeLineHeightConverter : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			double height = (double)values[ 0 ];
			float renderSize = (float)values[ 1 ];

			return height * renderSize;
		}

		public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
