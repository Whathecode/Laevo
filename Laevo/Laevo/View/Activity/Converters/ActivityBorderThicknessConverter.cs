using System;
using System.Windows;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityBorderThicknessConverter : AbstractMultiValueConverter<object, Thickness>
	{
		public override Thickness Convert( object[] values )
		{
			bool isOpen = (bool)values[ 0 ];
			bool isMouseOverContainer = (bool)values[ 1 ];
			bool isMouseOverButtons = (bool)values[ 2 ];
			double minWidth = (double)values[ 3 ];

			if ( isOpen && !(( isMouseOverContainer || isMouseOverButtons ) && minWidth > 2.0) )
			{
				return new Thickness( 4, 4, 0, 4 );
			}
			else
			{
				return new Thickness( isOpen ? 4 : 2 );
			}
		}

		public override object[] ConvertBack( Thickness value )
		{
			throw new NotImplementedException();
		}
	}
}
