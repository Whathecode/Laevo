using System;
using System.Windows;
using Laevo.ViewModel.Activity.LinkedActivity;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityCornerRadiusConverter : AbstractMultiValueConverter<object, CornerRadius>
	{
		public override CornerRadius Convert( object[] values )
		{
			const int rounded = 5;
			const int straight = 0;

			bool isOpen = (bool)values[ 0 ];
			var position = (ActivityPosition)values[ 1 ];

			int leftRadius = position.EqualsAny( ActivityPosition.Middle, ActivityPosition.End ) ? straight : rounded;
			int rightRadius = isOpen || position.EqualsAny( ActivityPosition.Start, ActivityPosition.Middle ) ? straight : rounded;

			return new CornerRadius( leftRadius, rightRadius, rightRadius, leftRadius );
		}

		public override object[] ConvertBack( CornerRadius value )
		{
			throw new NotImplementedException();
		}
	}
}