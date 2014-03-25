using System;
using System.Windows;
using Laevo.ViewModel.Activity.LinkedActivity;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityDashedBorderThicknessConverter : AbstractValueConverter<object, Thickness>
	{
		public override Thickness Convert( object value )
		{
			const int show = 2;
			const int hide = 0;

			var position = (ActivityPosition)value;

			int left = position.EqualsAny( ActivityPosition.Middle, ActivityPosition.End ) ? show : hide;
			int right = position.EqualsAny( ActivityPosition.Start, ActivityPosition.Middle ) ? show : hide;

			return new Thickness( left, hide, right, hide );
		}

		public override object ConvertBack( Thickness value )
		{
			throw new NotImplementedException();
		}
	}
}