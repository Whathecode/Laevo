using System;
using System.Globalization;
using System.Windows.Data;
using Whathecode.System.Extensions;


namespace Laevo.View.Activity.Converters
{
	public class IntervalOffsetConverter : IMultiValueConverter
	{
		double _availableHeight;

		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			double offsetPercentage = (double)values[ 0 ];
			double heightPercentage = (double)values[ 1 ];
			_availableHeight = 100 - (heightPercentage * 100); // Since TimePanel displays Y interval [0, 100].

			return _availableHeight - (_availableHeight * offsetPercentage);
		}

		public object[] ConvertBack( object offset, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			double offsetPercentage = ( (_availableHeight - (double)offset) / _availableHeight ).Clamp( 0, 1 );

			return new []
			{
				offsetPercentage,
				Binding.DoNothing
			};
		}
	}
}
