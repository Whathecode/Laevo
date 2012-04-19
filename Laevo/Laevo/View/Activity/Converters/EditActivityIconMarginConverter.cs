using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class EditActivityIconMarginConverter : IMultiValueConverter
	{
		public Thickness AddMargin { get; set; }

		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			double width = (double)values[ 0 ];
			double height = (double)values[ 1 ];

			return new Thickness(
				(-width / 2) - AddMargin.Left,
				(-height / 2) - AddMargin.Top,
				0, 0 );
		}

		public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
