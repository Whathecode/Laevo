using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Whathecode.System.Windows.Media.Extensions;


namespace Laevo.View.Activity.Converters
{
	class ActivityColorConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
		{
			var color = (Color)value;

			return color.Darken( 0.2 );
		}

		public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
