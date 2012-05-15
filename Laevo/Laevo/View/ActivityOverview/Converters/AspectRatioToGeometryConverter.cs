using System.Windows.Media.Media3D;


namespace Laevo.View.ActivityOverview.Converters
{
	public class AspectRatioToGeometryConverter : AbstractTimeLineWidthConverter
	{
		protected override object Convert( double windowWidth, double ratio, double planeWidth, object[] remainingValues )
		{
			// See TimeLineWidthConverter as well.
			var points = new[]
			{
				new Point3D( -ratio, 1, 0 ),	// Top left.
				new Point3D( -ratio, -1, 0 ),	// Bottom left.	
				new Point3D( planeWidth - ratio, -1, 0 ),	// Bottom right.
				new Point3D( planeWidth - ratio, 1, 0 ),		// Top right.
			};

			var pointCollection = new Point3DCollection( points );
			pointCollection.Freeze();
			return pointCollection;
		}
	}
}
