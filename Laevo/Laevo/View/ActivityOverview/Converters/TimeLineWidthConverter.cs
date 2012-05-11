namespace Laevo.View.ActivityOverview.Converters
{
	public class TimeLineWidthConverter : AbstractTimeLineWidthConverter
	{
		protected override object Convert( double windowWidth, double ratio, double planeWidth, object[] remainingValues )
		{
			float renderSize = (float)remainingValues[ 0 ];

			return windowWidth * (planeWidth / (ratio * 2)) * renderSize;
		}
	}
}
