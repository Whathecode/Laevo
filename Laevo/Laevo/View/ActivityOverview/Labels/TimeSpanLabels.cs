using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;


namespace Laevo.View.ActivityOverview.Labels
{
	/// <summary>
	///   Manages a set of labels indicating time intervals.
	/// </summary>
	/// <author>Steven Jeuris</author>
	class TimeSpanLabels : AbstractIntervalLabels<Line>
	{		
		public TimeSpanLabels(
			TimeLineControl timeLine,
			IInterval interval,
			Func<DateTime, bool> predicate )
			: base( timeLine, interval, predicate )
		{
		}


		protected override bool ShouldShowLabels()
		{
			return LabelsFitScreen();
		}

		protected override Line CreateNewLabel()
		{
			return new Line
			{
				X1 = 0,
				Y1 = 0,
				X2 = 0,
				Y2 = 500,
				Stroke = Brushes.White,
				StrokeThickness = 1,
				HorizontalAlignment = HorizontalAlignment.Center
			};
		}

		protected override void UpdateLabel( Line label, DateTime occurance )
		{
			long minimumTicks = Interval.MinimumInterval.Ticks;
			double minimumWidth = (double)minimumTicks / TimeLine.GetVisibleTicks() * TimeLine.ActualWidth;
			const double maxHeight = 2000;  // TODO: Limit to maximum screen height.
			label.Y2 = minimumWidth > maxHeight ? maxHeight : minimumWidth;
		}
	}
}
