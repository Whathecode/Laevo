using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;


namespace Laevo.View.Activity.Converters
{
	public class AttentionTimeSpanConverter : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			// Early out when control is disconnected from the items control.
			// This still happens since this converter binds to `DataContext`:
			// http://stackoverflow.com/questions/3868786/wpf-sentinel-objects-and-how-to-check-for-an-internal-type#comment46479068_3868786
			if ( DependencyProperty.UnsetValue.EqualsAny( values ) )
			{
				return new TimeInterval( Interval<DateTime, TimeSpan>.Empty );
			}

			var attentionSpan = (TimeInterval)values[ 0 ];
			var occurance = (DateTime)values[ 1 ];
			var timeSpan = (TimeSpan)values[ 2 ];
			double width = (double)values[ 3 ];

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
