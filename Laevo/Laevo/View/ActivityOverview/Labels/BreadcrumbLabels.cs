using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Linq;


namespace Laevo.View.ActivityOverview.Labels
{
	class BreadcrumbLabels : AbstractTopLabels
	{
		const double HorizontalLabelOffset = 13.0;
		const double VerticalLabelOffset = -60.0;


		public BreadcrumbLabels( TimeLineControl timeLine )
			: base( timeLine )
		{
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

		protected override DateTime[] GetTopLabelPositions( Interval<DateTime> interval )
		{
			// TODO: Performance issue with string formatting.
			IEnumerable<DateTime> flattened = Intervals
				.TakeWhile( i => i != CurrentDepth )
				.SelectMany( i => i.GetPositions( interval ) )
				.OrderBy( d => d )
				.Distinct( d => Formatting[ CurrentDepth ]( d ) );

			if ( flattened.Count() >= 2 )
			{
				// Only keep one position which lies in front of the time line.
				return flattened
					.Zip( flattened.Skip( 1 ), Tuple.Create )
					.SkipWhile( t => t.Item2 <= TimeLine.VisibleInterval.Start )
					.Select( t => t.Item1 )
					.Concat( new [] { flattened.Last() } ) // Don't forget the last! It was dropped when zipping with Skip().
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
