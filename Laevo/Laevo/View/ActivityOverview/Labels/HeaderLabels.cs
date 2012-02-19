using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.ActivityOverview.Labels
{
	class HeaderLabels : AbstractLabels<TextBlock>
	{
		const double HorizontalLabelOffset = 10.0;
		const double VerticalLabelOffset = 15.0;

		/// <summary>
		///   Intervals which are shown, from large to small.
		/// </summary>
		readonly RegularInterval[] _intervals;

		/// <summary>
		///   The smallest interval which is too large to fit the screen vertically. This is the most relevant 'depth'.
		/// </summary>
		RegularInterval _currentDepth;
		readonly Dictionary<TextBlock, RegularInterval> _matchLabelsToDepth = new Dictionary<TextBlock, RegularInterval>();


		public HeaderLabels( TimeLineControl timeLine, params RegularInterval[] intervals )
			: base( timeLine )
		{
			_intervals = intervals;
		}


		protected override bool ShouldShowLabels()
		{
			return IsIntervalHigherThanScreen( _intervals[ 0 ] );
		}

		protected override bool IsVisible( TextBlock label, DateTime occurance )
		{
			return _currentDepth != null
				? _matchLabelsToDepth[ label ] == _currentDepth
				: false;
		}

		bool IsIntervalHigherThanScreen( RegularInterval interval )
		{
			return (double)interval.Interval.Ticks / TimeLine.GetVisibleTicks() * TimeLine.ActualWidth > TimeLine.ActualHeight;
		}

		protected override IEnumerable<DateTime> GetPositions( Interval<DateTime> interval )
		{
			// Get the smallest interval which is currently the most relevant.
			_currentDepth = _intervals.Reverse().FirstOrDefault( IsIntervalHigherThanScreen );

			return _currentDepth != null
				? _currentDepth.GetPositions( interval )
				: new DateTime[] { };
		}

		protected override TextBlock CreateNewLabel()
		{
			return new TextBlock()
			{
				Foreground = Brushes.White,
				FontSize = 70
			};
		}

		protected override void UpdateLabel( TextBlock label )
		{
			_matchLabelsToDepth[ label ] = _currentDepth;

			label.Text = "Test";
			label.SetValue( TimeLineControl.OffsetProperty, TimeLine.ActualHeight - label.ActualHeight + VerticalLabelOffset );
			label.SetValue(
				FrameworkElement.MarginProperty,
				new Thickness( (label.ActualWidth / 2) + HorizontalLabelOffset, 0, 0, 0 ) );			
		}
	}
}
