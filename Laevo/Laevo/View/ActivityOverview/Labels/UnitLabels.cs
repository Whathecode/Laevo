﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Laevo.View.ActivityOverview.Labels
{
	class UnitLabels : AbstractIntervalLabels<TextBlock>
	{
		const double HorizontalLabelOffset = 5.0;

		public string FormatString { get; private set; }
		readonly Func<bool> _predicate;
		readonly double _offset;
		readonly double _fontSize;


		public UnitLabels( TimeLineControl timeLine, IInterval interval, string formatString, Func<bool> predicate, double offset = 0, double fontSize = 20 )
			: base( timeLine, interval, d => true, interval.MinimumInterval )
		{
			FormatString = formatString;
			_predicate = predicate;
			_offset = offset;
			_fontSize = fontSize;
		}


		public override bool ShouldShowLabels()
		{
			return LabelsFitScreen() && _predicate();
		}

		protected override TextBlock CreateNewLabel()
		{
			return new TextBlock
			{
				Foreground = Brushes.White,
				FontSize = _fontSize,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness( HorizontalLabelOffset, 0, 0, 0 ),
				IsHitTestVisible = false
			};
		}

		protected override void InitializeLabel( TextBlock label, DateTime occurance )
		{
			label.Text = occurance.ToString( FormatString );
			label.FontSize = _fontSize;
			label.SetValue( TimeLineControl.OffsetProperty, _offset );
		}

		protected override void UpdateLabel( TextBlock label )
		{
			// Nothing to do.
		}
	}
}
