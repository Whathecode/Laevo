using System;
using System.Globalization;
using System.Windows.Data;
using Visibility = System.Windows.Visibility;


namespace Laevo.View.Common.Converters
{
	class NumberToVisibilityConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
		{
			return (int)value > 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
		{
			return value;
		}
	}
}