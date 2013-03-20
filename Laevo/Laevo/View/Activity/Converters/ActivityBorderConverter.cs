using System;
using System.Windows.Media;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityBorderConverter : AbstractMultiValueConverter<bool, Brush>
	{
		public override Brush Convert( bool[] value )
		{
			bool isActive = value[ 0 ];
			bool isOpen = value[ 1 ];
			bool hasOpenWindows = value[ 2 ];

			return isActive ? Brushes.Yellow : ( hasOpenWindows && !isOpen ? Brushes.DarkOrange : Brushes.White );
		}

		public override bool[] ConvertBack( Brush value )
		{
			throw new NotImplementedException();
		}
	}
}
