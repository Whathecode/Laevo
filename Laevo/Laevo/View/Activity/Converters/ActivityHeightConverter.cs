using System;
using Laevo.View.ActivityOverview;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class ActivityHeightConverter : AbstractMultiValueConverter<double, double>
	{
		public override double Convert( double[] values )
		{
			double heightPercentage = values[ 0 ];
			double headerHeight = values[ 1 ];
			double availableHeight = values[ 2 ] - ActivityOverviewWindow.TopOffset - ActivityOverviewWindow.BottomOffset;

			double size = (availableHeight * heightPercentage) - headerHeight;
			return size < 0 ? 0 : size;
		}

		public override double[] ConvertBack( double value )
		{
			throw new NotSupportedException();
		}
	}
}
