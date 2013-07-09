using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityBorderConverter : AbstractMultiValueConverter<bool, Brush>
	{
		readonly Dictionary<Brush, Brush> _unattendBrushes = new Dictionary<Brush, Brush>();


		public ActivityBorderConverter()
		{
			var borderBrushes = new[] { Brushes.DarkOrange, Brushes.White };
			foreach ( var brush in borderBrushes )
			{
				var unattendedBrush = new SolidColorBrush( Colors.Yellow );
				var animateColor = new ColorAnimation( Colors.Yellow, brush.Color, new Duration( new TimeSpan( 0, 0, 0, 0, 500 ) ) )
				{
					AutoReverse = true,
					RepeatBehavior = RepeatBehavior.Forever
				};
				unattendedBrush.BeginAnimation( SolidColorBrush.ColorProperty, animateColor );
				_unattendBrushes.Add( brush, unattendedBrush );
			}
		}


		public override Brush Convert( bool[] value )
		{
			bool isActive = value[ 0 ];
			bool isOpen = value[ 1 ];
			bool hasOpenWindows = value[ 2 ];
			bool hasUnattendedInterruptions = value[ 3 ];

			if ( isActive )
			{
				return Brushes.Yellow;
			}
			else
			{
				Brush desiredBrush = hasOpenWindows && !isOpen ? Brushes.DarkOrange : Brushes.White;
				return hasUnattendedInterruptions ? _unattendBrushes[ desiredBrush ] : desiredBrush;
			}
		}

		public override bool[] ConvertBack( Brush value )
		{
			throw new NotImplementedException();
		}
	}
}
