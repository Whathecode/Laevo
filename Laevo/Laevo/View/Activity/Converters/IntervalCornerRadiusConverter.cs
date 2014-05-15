using System;
using System.Windows;
using Laevo.ViewModel.Activity;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class IntervalCornerRadiusConverter : AbstractMultiValueConverter<object, CornerRadius>
	{
		public override CornerRadius Convert( object[] values )
		{
			const int rounded = 5;
			const int straight = 0;

			bool isOpen = (bool)values[ 0 ];
			var position = (ActivityPosition)values[ 1 ];
			bool isPlanned = (bool)values[ 2 ];

			int leftRadius = position.HasFlag( ActivityPosition.Start ) ? rounded : straight;
			int rightRadius = position.HasFlag( ActivityPosition.End ) && (!isOpen || isPlanned) ? rounded : straight;

			return new CornerRadius( leftRadius, rightRadius, rightRadius, leftRadius );
		}

		public override object[] ConvertBack( CornerRadius value )
		{
			throw new NotImplementedException();
		}
	}
}