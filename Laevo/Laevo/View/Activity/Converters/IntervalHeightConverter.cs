using System;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class IntervalHeightConverter : AbstractMultiValueConverter<double, double>
	{
		public override double Convert( double[] values )
		{
			double headerHeight = values[ 0 ];
			double availableHeight = values[ 1 ];

			double size = availableHeight - headerHeight;
			return size < 0 ? 0 : size;
		}

		public override double[] ConvertBack( double value )
		{
			throw new NotSupportedException();
		}
	}
}
