using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.Activity.Converters
{
	public class AttentionTimeSpanConverter : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			var attentionSpan = (TimeInterval)values[ 0 ];
			var occurance = (DateTime)values[ 1 ];
			var timeSpan = (TimeSpan)values[ 2 ];
			double width = values[ 3 ] == DependencyProperty.UnsetValue ? 0 : (double)values[ 3 ];

			return new TimeInterval( occurance, occurance + timeSpan ).Map(
				parameter.Equals( "Start" ) ? attentionSpan.Start : attentionSpan.End,
				new Interval<double>( 0, width ) );
		}

		public object[] ConvertBack( object offset, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
