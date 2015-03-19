using System;
using System.Globalization;
using System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	/// <summary>
	///   Converter which returns null rather than {DisconnectedItem} when binding to DataContext.
	/// </summary>
	class DisconnectedConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
		{
			if ( value == BindingOperations.DisconnectedSource )
			{
				return null;
			}

			return value;
		}

		public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
		{
			return value;
		}
	}
}
