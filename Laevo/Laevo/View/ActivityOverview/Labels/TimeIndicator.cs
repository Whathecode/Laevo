using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;


namespace Laevo.View.ActivityOverview.Labels
{
	public class TimeIndicator : Canvas
	{
		static readonly Color Color = Colors.Yellow;
		const double TopOffset = ActivityOverviewWindow.TopOffset - 10;
		const double TriangleWidth = 20;
		const double TriangleHeight = 20;

		public TimeIndicator()
		{			
			HorizontalAlignment = HorizontalAlignment.Center;
			SetValue( ZIndexProperty, 20 );
			var lineBrush = new LinearGradientBrush( Color, Colors.Transparent, 90 );
			lineBrush.Freeze();

			var heightBinding = new Binding( "ActualHeight" ) { Source = this };
			const double halfTriangle = TriangleWidth / 2;

			// Line.
			var line = new Line
			{
				X1 = 0,
				Y1 = 0,
				X2 = 0,
				Stroke = lineBrush,
				StrokeThickness = 2,				
				IsHitTestVisible = false
			};
			line.SetValue( TopProperty, TopOffset );
			line.SetBinding( Line.Y2Property, heightBinding );
			Children.Add( line );

			// Top triangle.
			var triangleBrush = new SolidColorBrush( Color );
			triangleBrush.Freeze();
			var topTriangle = new Polygon
			{
				Points = { new Point( -halfTriangle, 0 ), new Point( halfTriangle, 0 ), new Point( 0, TriangleHeight ) },
				Fill = triangleBrush
			};
			topTriangle.SetValue( TopProperty, TopOffset );
			Children.Add( topTriangle );
		}
	}
}
