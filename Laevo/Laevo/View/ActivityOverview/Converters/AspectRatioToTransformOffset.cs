using System;
using System.Globalization;
using System.Windows.Data;


namespace Laevo.View.ActivityOverview.Converters
{
	public class AspectRatioToTransformOffset : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			double ratio = (double)values[ 0 ] / (double)values[ 1 ];
			return -ratio;
		}

		public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
