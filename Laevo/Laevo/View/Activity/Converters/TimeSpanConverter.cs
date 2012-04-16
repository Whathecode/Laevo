using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.Activity.Converters
{
	public class TimeSpanConverter : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			var attentionSpan = (Interval<DateTime>)values[ 0 ];
			DateTime occurance = (DateTime)values[ 1 ];
			TimeSpan timeSpan = (TimeSpan)values[ 2 ];
			double width = values[ 3 ] == DependencyProperty.UnsetValue ? 0 : (double)values[ 3 ];

			return new Interval<long>( occurance.Ticks, occurance.Ticks + timeSpan.Ticks ).Map(
				parameter.Equals( "Start" ) ? attentionSpan.Start.Ticks : attentionSpan.End.Ticks,
				new Interval<double>( 0, width ) );
		}

		public object[] ConvertBack( object offset, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
