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
	abstract class AbstractTimeSpanLabels : AbstractLabels<Line>
	{
		const double MinimumSpaceBetweenLabels = 20.0;


		protected AbstractTimeSpanLabels( TimeLineControl timeLine )
			: base( timeLine )
		{
		}


		protected override bool ShouldShowLabels()
		{
			long minimumTicks = GetMinimumTimeSpan().Ticks;
			int maximumLabels = (int)Math.Ceiling( TimeLine.ActualWidth / MinimumSpaceBetweenLabels );
			return TimeLine.GetVisibleTicks() / minimumTicks < maximumLabels;
		}

		protected override bool IsVisible( Line label, DateTime occurance )
		{
			return TimeLine.VisibleInterval.LiesInInterval( occurance );
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

		protected override void UpdateLabel( Line label )
		{
			long minimumTicks = GetMinimumTimeSpan().Ticks;
			double minimumWidth = (double)minimumTicks / TimeLine.GetVisibleTicks() * TimeLine.ActualWidth;
			label.Y2 = minimumWidth;
		}

		/// <summary>
		///   Gets the minimum time span between each label.
		/// </summary>
		protected abstract TimeSpan GetMinimumTimeSpan();
	}
}
