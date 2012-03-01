using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Media3D;


namespace Laevo.View.ActivityOverview
{
	public class AspectRatioToGeometryConverter : IMultiValueConverter
	{
		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			double ratio = (double)values[ 0 ] / (double)values[ 1 ];

			var points = new[]
			{
				new Point3D( 0, 1, 0 ),			// Top left.
				new Point3D( 0, -1, 0 ),		// Bottom left.							
				new Point3D( ratio * 2, -1, 0 ),// Bottom right.
				new Point3D( ratio * 2, 1, 0 ),	// Top right.	
			};

			return new Point3DCollection( points );
		}

		public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}
