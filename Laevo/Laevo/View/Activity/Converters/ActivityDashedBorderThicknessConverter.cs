using System;
using System.Windows;
using Laevo.ViewModel.Activity.LinkedActivity;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityDashedBorderThicknessConverter : AbstractMultiValueConverter<object, Thickness>
	{
		public override Thickness Convert( object[] values )
		{
			const int fb = 2;
			const int tb = 2;

			bool isOpen = (bool)values[ 0 ];
			bool isMouseOverContainer = (bool)values[ 1 ];
			bool isMouseOverButtons = (bool)values[ 2 ];
			double minWidth = (double)values[ 3 ];
			ActivityPosition position = (ActivityPosition)values[ 4 ];

			if ( isOpen )
			{
				if ( ( isMouseOverContainer || isMouseOverButtons ) && minWidth > 2.0 )
				{
					switch ( position )
					{
						case ActivityPosition.None:
							return new Thickness( 0, 0, fb, 0 );
						case ActivityPosition.End:
							return new Thickness( fb, 0, fb, 0 );
					}
				}
				switch ( position )
				{
					case ActivityPosition.None:
						return new Thickness( 0 );
					case ActivityPosition.Start:
						return new Thickness( 0, 0, fb, 0 );
					case ActivityPosition.Middle:
						return new Thickness( fb, 0, fb, 0 );
					case ActivityPosition.End:
						return new Thickness( fb, 0, 0, 0 );
					default:
						return new Thickness( 0 );
				}
			}
			switch ( position )
			{
				case ActivityPosition.None:
					return new Thickness( 0 );
				case ActivityPosition.Start:
					return new Thickness( 0, 0, tb, 0 );
				case ActivityPosition.Middle:
					return new Thickness( tb, 0, tb, 0 );
				case ActivityPosition.End:
					return new Thickness( tb, 0, 0, 0 );
				default:
					return new Thickness( 0 );
			}
		}

		public override object[] ConvertBack( Thickness value )
		{
			throw new NotImplementedException();
		}
	}
}