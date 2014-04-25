using System;
using System.Windows;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class ActivityDownIconMarginConverter : AbstractMultiValueConverter<double, Thickness>
	{
		public override Thickness Convert( double[] values )
		{
			double iconwidth = values[ 0 ];
			double iconHeight = values[ 1 ];
			double controlHeight = values[ 2 ];

			return new Thickness( -iconwidth / 3, iconHeight + 8, 0, 0 );
		}

		public override double[] ConvertBack( Thickness value )
		{
			throw new NotSupportedException();
		}
	}
}
