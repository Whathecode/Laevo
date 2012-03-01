using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Windows;


namespace Laevo.View.ActivityOverview.Labels
{
	abstract class AbstractTopLabels : AbstractLabels<TextBlock>
	{
		/// <summary>
		///   Intervals which are shown, from large to small.
		/// </summary>
		protected readonly List<IInterval> Intervals = new List<IInterval>();

		/// <summary>
		///   Formatting strings for the intervals.
		/// </summary>
		protected readonly Dictionary<IInterval, Func<DateTime, string>> Formatting
			= new Dictionary<IInterval, Func<DateTime, string>>();

		/// <summary>
		///   The smallest interval which is too large to fit the screen vertically. This is the most relevant 'depth'.
		/// </summary>
		protected IInterval CurrentDepth;

		DateTime _earliestLabelTime;
		DateTime? _secondLabelTime;
		readonly TextBlock _earliestLabel;

		readonly Dictionary<TextBlock, IInterval> _matchLabelsToDepth = new Dictionary<TextBlock, IInterval>();


		protected AbstractTopLabels( TimeLineControl timeLine )
			: base( timeLine, TimeSpan.Zero )
		{
			_earliestLabel = CreateNewLabelInner();
			TimeLine.Children.Add( _earliestLabel );
		}


		public void AddInterval( IInterval interval, string formatString )
		{
			AddInterval( interval, d => d.ToString( formatString ) );
		}

		public void AddInterval( IInterval interval, Func<DateTime, string> formatDate )
		{
			Intervals.Add( interval );
			Formatting[ interval ] = formatDate;			
		}

		TextBlock CreateNewLabelInner()
		{
			return CreateNewLabel();
		}

		protected override bool ShouldShowLabels()
		{
			// These labels are always visible.
			return true;
		}

		protected override bool IsVisible( TextBlock label, DateTime occurance )
		{
			return CurrentDepth != null && _matchLabelsToDepth[ label ] == CurrentDepth;
		}

		bool IsIntervalHigherThanScreen( IInterval interval )
		{
			return (double)interval.MinimumInterval.Ticks / TimeLine.GetVisibleTicks() * TimeLine.ActualWidth > TimeLine.ActualHeight;
		}

		protected override IEnumerable<DateTime> GetPositions( Interval<DateTime> interval )
		{
			// Get the smallest interval which is currently the most relevant.
			CurrentDepth = ((IEnumerable<IInterval>)Intervals).Reverse().FirstOrDefault( IsIntervalHigherThanScreen );
			if ( CurrentDepth == null )
			{
				_earliestLabel.Visibility = Visibility.Hidden;
				return new DateTime[] { };
			}
			DateTime[] positions = GetTopLabelPositions( interval );
			if ( positions.Length == 0 )
			{
				_earliestLabel.Visibility = Visibility.Hidden;
				return new DateTime[] { };
			}

			// Retrieve the relevant positions to position the labels.			
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
			_earliestLabel.Visibility = Visibility.Visible;
			_earliestLabel.SetValue( TimeLineControl.OccuranceProperty, positionTime );

			return positions;
		}

		protected override void UpdateLabel( TextBlock label, DateTime occurance )
		{
			_matchLabelsToDepth[ label ] = CurrentDepth;

			// Show actual label when it doesn't overlap with the earliest label.
			label.Visibility = TimeLine.VisibleInterval.LiesInInterval( occurance ) ? Visibility.Visible : Visibility.Hidden;
			UpdateText( label, occurance );
		}

		void UpdateText( TextBlock label, DateTime occurance )
		{
			if ( CurrentDepth != null )
			{
				label.Text = Formatting[ CurrentDepth ]( occurance );
				UpdateTopLabel( label );
			}
		}

		protected abstract DateTime[] GetTopLabelPositions( Interval<DateTime> interval );

		protected abstract void UpdateTopLabel( TextBlock block );
	}
}
