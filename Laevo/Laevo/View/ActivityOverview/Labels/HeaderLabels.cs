using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.ActivityOverview.Labels
{
	class HeaderLabels : AbstractTopLabels
	{
		const double HorizontalLabelOffset = 10.0;
		const double VerticalLabelOffset = 15.0;


		public HeaderLabels( TimeLineControl timeLine )
			: base( timeLine ) { }


		protected override TextBlock CreateNewLabel()
		{
			return new TextBlock
			{
				Foreground = Brushes.White,
				FontSize = 70,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness( HorizontalLabelOffset, 0, HorizontalLabelOffset, 0 )
			};
		}

		protected override DateTime[] GetTopLabelPositions( Interval<DateTime> interval )
		{
			return CurrentDepth.GetPositions( interval ).ToArray();
		}

		protected override void UpdateTopLabel( TextBlock block )
		{
			block.SetValue( TimeLineControl.OffsetProperty, TimeLine.ActualHeight - block.ActualHeight + VerticalLabelOffset );
		}
	}
}
