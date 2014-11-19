using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;
using Whathecode.System.Linq;


namespace Laevo.View.ActivityOverview.Labels
{
	class BreadcrumbLabels : AbstractTopLabels
	{
		const double HorizontalLabelOffset = 13.0;
		const double VerticalLabelOffset = -60.0;

		bool _currentDepthChanged;


		public BreadcrumbLabels( TimeLineControl timeLine )
			: base( timeLine )
		{
			CurrentDepthChanged += () => _currentDepthChanged = true;
		}


		protected override TextBlock CreateNewLabel()
		{
			return new TextBlock
			{
				Foreground = Brushes.WhiteSmoke,
				FontSize = 30,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness( HorizontalLabelOffset, 0, HorizontalLabelOffset, 0 ),
				IsHitTestVisible = false
			};
		}

		readonly Dictionary<DateTime, string> _formattedDates = new Dictionary<DateTime, string>();
		protected override DateTime[] GetTopLabelPositions( TimeInterval interval )
		{
			if ( _currentDepthChanged )
			{
				// Different depths may use different formats.
				_formattedDates.Clear();
				_currentDepthChanged = false;
			}

			DateTime[] flattened = Intervals
				.TakeWhile( i => i != CurrentDepth )
				.SelectMany( i => i.GetPositions( interval ) )
				.ToArray();

			// Update the list of formatted dates.
			var toRemove = _formattedDates.Keys.Where( d => !flattened.Contains( d ) ).ToArray();
			toRemove.ForEach( d => _formattedDates.Remove( d ) );
			var toAdd = flattened.Where( d => !_formattedDates.ContainsKey( d ) ).ToArray();
			toAdd.ForEach( d => _formattedDates[ d ] = Formatting[ CurrentDepth ]( d ) );

			// Filter out the dates which are represented similarly.
			var filtered = _formattedDates
				.OrderBy( p => p.Key )
				.Distinct( p => p.Value )
				.Select( p => p.Key );

			if ( filtered.Count() >= 2 )
			{
				// Only keep one position which lies in front of the time line.
				DateTime start = TimeLine.VisibleInterval.Start;
				return filtered
					.Zip( filtered.Skip( 1 ), Tuple.Create )
					.SkipWhile( t => t.Item2 <= start )
					.Select( t => t.Item1 )
					.Concat( new [] { filtered.Last() } ) // Don't forget the last! It was dropped when zipping with Skip().
					.ToArray();
			}

			return flattened.ToArray();
		}

		protected override void UpdateTopLabel( TextBlock block )
		{
			block.SetValue( TimeLineControl.OffsetProperty, TimeLine.ActualHeight - block.ActualHeight + VerticalLabelOffset );
		}
	}
}
