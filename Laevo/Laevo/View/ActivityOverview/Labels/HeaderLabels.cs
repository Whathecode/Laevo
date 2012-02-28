using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Windows;


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

		DateTime _earliestLabelTime;
		DateTime? _secondLabelTime;
		readonly TextBlock _earliestLabel;		

		readonly Dictionary<TextBlock, RegularInterval> _matchLabelsToDepth = new Dictionary<TextBlock, RegularInterval>();


		public HeaderLabels( TimeLineControl timeLine, params RegularInterval[] intervals )
			: base( timeLine )
		{
			_intervals = intervals;

			if ( _earliestLabel == null )
			{
				_earliestLabel = CreateNewLabel();
				TimeLine.Children.Add( _earliestLabel );
			}
		}


		protected override bool ShouldShowLabels()
		{
			// These labels are always visible.
			return true;
		}

		protected override bool IsVisible( TextBlock label, DateTime occurance )
		{
			return _currentDepth != null && _matchLabelsToDepth[ label ] == _currentDepth;
		}

		bool IsIntervalHigherThanScreen( RegularInterval interval )
		{
			return (double)interval.Interval.Ticks / TimeLine.GetVisibleTicks() * TimeLine.ActualWidth > TimeLine.ActualHeight;
		}

		protected override IEnumerable<DateTime> GetPositions( Interval<DateTime> interval )
		{
			// Get the smallest interval which is currently the most relevant.
			_currentDepth = _intervals.Reverse().FirstOrDefault( IsIntervalHigherThanScreen );

			if ( _currentDepth == null )
			{
				_earliestLabel.Visibility = Visibility.Hidden;
				return new DateTime[] { };
			}
			else
			{
				_earliestLabel.Visibility = Visibility.Visible;
			}

			DateTime[] positions = _currentDepth.GetPositions( interval ).ToArray();
			_earliestLabelTime = positions[ 0 ];
			_secondLabelTime = positions.Length > 1 ? (DateTime?)positions[ 1 ] : null;

			// Position earliest label on the top left, pushing it of the screen by the second label when scrolling.
			UpdateText( _earliestLabel, _earliestLabelTime );
			DateTime positionTime = TimeLine.VisibleInterval.Start;
			if ( _secondLabelTime != null )
			{
				long ticksFromLeft = _secondLabelTime.Value.Ticks - TimeLine.VisibleInterval.Start.Ticks;
				_earliestLabel.Measure( SizeHelper.MaxSize );
				long requiredTicks = (long)((_earliestLabel.DesiredSize.Width / TimeLine.ActualWidth) * TimeLine.GetVisibleTicks());
				long ticksOffset = ticksFromLeft < requiredTicks ? requiredTicks - ticksFromLeft : 0;
				positionTime -= new TimeSpan( ticksOffset );
			}
			_earliestLabel.SetValue( TimeLineControl.OccuranceProperty, positionTime );

			return positions;
		}

		protected override sealed TextBlock CreateNewLabel()
		{
			return new TextBlock
			{
				Foreground = Brushes.White,
				FontSize = 70,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness( HorizontalLabelOffset, 0, HorizontalLabelOffset, 0 )
			};
		}

		protected override void UpdateLabel( TextBlock label )
		{
			_matchLabelsToDepth[ label ] = _currentDepth;
			
			// Show actual label when it doesn't overlap with the earliest label.
			var occurance = (DateTime)label.GetValue( TimeLineControl.OccuranceProperty );
			label.Visibility = TimeLine.VisibleInterval.LiesInInterval( occurance ) ? Visibility.Visible : Visibility.Hidden;			
			UpdateText( label, occurance );
		}

		void UpdateText( TextBlock block, DateTime occurance )
		{
			if ( _currentDepth != null )
			{
				block.Text = _currentDepth.Format(occurance);
				block.SetValue( TimeLineControl.OffsetProperty, TimeLine.ActualHeight - block.ActualHeight + VerticalLabelOffset );
			}
		}
	}
}
