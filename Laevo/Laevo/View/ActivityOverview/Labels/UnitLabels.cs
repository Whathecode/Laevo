using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Laevo.View.ActivityOverview.Labels
{
	class UnitLabels : AbstractIntervalLabels<TextBlock>
	{
		const double HorizontalLabelOffset = 5.0;

		readonly string _formatString;
		readonly Func<bool> _predicate;


		public UnitLabels( TimeLineControl timeLine, IInterval interval, string formatString, Func<bool> predicate )
			: base( timeLine, interval, d => true )
		{
			_formatString = formatString;
			_predicate = predicate;
		}


		protected override bool ShouldShowLabels()
		{
			return LabelsFitScreen() && _predicate();
		}

		protected override TextBlock CreateNewLabel()
		{
			return new TextBlock
			{
				Foreground = Brushes.White,
				FontSize = 20,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness( HorizontalLabelOffset, 0, 0, 0 )
			};
		}

		protected override void UpdateLabel( TextBlock label, DateTime occurance )
		{
			label.Text = occurance.ToString( _formatString );
		}
	}
}
