using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class BorderBrushConverter : AbstractMultiValueConverter<bool, Brush>
	{
		readonly Dictionary<Brush, Brush> _interruptionBrushes = new Dictionary<Brush, Brush>();


		public BorderBrushConverter()
		{
			var borderBrushes = new[] { Brushes.DarkOrange, Brushes.White };
			foreach ( var brush in borderBrushes )
			{
				var interruptionBrush = new SolidColorBrush( Colors.Yellow );
				var animateColor = new ColorAnimation( Colors.Yellow, brush.Color, new Duration( new TimeSpan( 0, 0, 0, 0, 500 ) ) )
				{
					AutoReverse = true,
					RepeatBehavior = RepeatBehavior.Forever
				};
				interruptionBrush.BeginAnimation( SolidColorBrush.ColorProperty, animateColor );
				_interruptionBrushes.Add( brush, interruptionBrush );
			}
		}


		public override Brush Convert( bool[] value )
		{
			bool isActive = value[ 0 ];
			bool needsSuspension = value[ 1 ];
			bool hasUnattendedInterruptions = value[ 2 ];
			bool isOpen = value[ 3 ];

			if ( isActive )
			{
				return Brushes.Yellow;
			}
			else
			{
				Brush desiredBrush = needsSuspension && !isOpen ? Brushes.DarkOrange : Brushes.White;
				return hasUnattendedInterruptions ? _interruptionBrushes[ desiredBrush ] : desiredBrush;
			}
		}

		public override bool[] ConvertBack( Brush value )
		{
			throw new NotImplementedException();
		}
	}
}
