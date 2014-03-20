using System;
using System.Windows;
using Laevo.ViewModel.Activity.LinkedActivity;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityBorderThicknessConverter : AbstractMultiValueConverter<object, Thickness>
	{
		public override Thickness Convert( object[] values )
		{
			const int fb = 4;
			const int tb = 2;

			bool isOpen = (bool)values[ 0 ];
			ActivityPosition position = (ActivityPosition)values[ 1 ];

			if ( isOpen )
			{
				switch ( position )
				{
					case ActivityPosition.None:
						return new Thickness( fb, fb, 0, fb );
					case ActivityPosition.Start:
						return new Thickness( fb, fb, 0, fb );
					case ActivityPosition.Middle:
						return new Thickness( 0, fb, 0, fb );
					case ActivityPosition.End:
						return new Thickness( 0, fb, 0, fb );
					default:
						return new Thickness( fb );
				}
			}
			switch ( position )
			{
				case ActivityPosition.None:
					return new Thickness( tb );
				case ActivityPosition.Start:
					return new Thickness( tb, tb, 0, tb );
				case ActivityPosition.Middle:
					return new Thickness( 0, tb, 0, tb );
				case ActivityPosition.End:
					return new Thickness( 0, tb, tb, tb );
				default:
					return new Thickness( tb );
			}
		}

		public override object[] ConvertBack( Thickness value )
		{
			throw new NotImplementedException();
		}
	}
}