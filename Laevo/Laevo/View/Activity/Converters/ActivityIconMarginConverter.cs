using System;
using System.Windows;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class ActivityIconMarginConverter : AbstractMultiValueConverter<double, Thickness>
	{
		public override Thickness Convert( double[] values )
		{
			double width = values[ 0 ];
			double height = values[ 1 ];

			return new Thickness( -width / 2, 0, 0, 0 );
		}

		public override double[] ConvertBack( Thickness value )
		{
			throw new NotSupportedException();
		}
	}
}
