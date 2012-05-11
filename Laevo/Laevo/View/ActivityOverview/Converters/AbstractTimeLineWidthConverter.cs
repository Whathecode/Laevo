using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Whathecode.System;
using Whathecode.System.Extensions;
using Whathecode.System.Linq;


namespace Laevo.View.ActivityOverview.Converters
{
	public abstract class AbstractTimeLineWidthConverter : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			double windowWidth = (double)values[ 0 ];
			double windowHeight = (double)values[ 1 ];
			double ratio = windowWidth / windowHeight;
			double angle = (double)values[ 2 ];

			// Calculate the required plane width by using line intersection of the right end of the camera view with the plane.			
			double angleRadians = MathHelper.DegreesToRadians( angle );
			Vector leftFieldOfView = new Vector( -ratio, 0 );
			VectorLine planeLine = new VectorLine( leftFieldOfView, new Vector( 0, Math.Tan( angleRadians ) * -ratio ) );
			VectorLine rightFieldOfViewLine = new VectorLine( new Vector( 0, 1 ), new Vector( ratio, 0 ) );
			Vector intersection = planeLine.Intersection( rightFieldOfViewLine );
			double planeWidth = leftFieldOfView.DistanceTo( intersection );

			return Convert( windowWidth, ratio, planeWidth, values.Skip( 3 ).ToArray() );
		}

		protected abstract object Convert( double windowWidth, double ratio, double planeWidth, object[] remainingValues );

		public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}		
	}
}
