using System;
using System.Windows;
using Laevo.ViewModel.Activity.LinkedActivity;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityCornerRadiusConverter : AbstractMultiValueConverter<object, CornerRadius>
	{
		public override CornerRadius Convert( object[] values )
		{
			const int cr = 5;

			bool isOpen = (bool)values[ 0 ];
			bool isMouseOverContainer = (bool)values[ 1 ];
			bool isMouseOverButtons = (bool)values[ 2 ];
			double minWidth = (double)values[ 3 ];
			ActivityPosition position = (ActivityPosition)values[ 4 ];

			// TODO: Why check if mouse over?
			if ( isOpen ) //&& !(( isMouseOverContainer || isMouseOverButtons ) && minWidth > _cr._cr) )
			{
				switch ( position )
				{
					case ActivityPosition.None:
						return new CornerRadius( cr, 0, 0, cr );
					case ActivityPosition.Start:
						return new CornerRadius( cr, 0, 0, cr );
					case ActivityPosition.Middle:
						return new CornerRadius( 0, 0, 0, 0 );
					case ActivityPosition.End:
						return new CornerRadius( 0, 0, 0, 0 );
					default:
						return new CornerRadius( cr );
				}
			}
			switch ( position )
			{
				case ActivityPosition.None:
					return new CornerRadius( cr );
				case ActivityPosition.Start:
					return new CornerRadius( cr, 0, 0, cr );
				case ActivityPosition.Middle:
					return new CornerRadius( 0, 0, 0, 0 );
				case ActivityPosition.End:
					return new CornerRadius( 0, cr, cr, 0 );
				default:
					return new CornerRadius( cr );
			}
		}

		public override object[] ConvertBack( CornerRadius value )
		{
			throw new NotImplementedException();
		}
	}
}