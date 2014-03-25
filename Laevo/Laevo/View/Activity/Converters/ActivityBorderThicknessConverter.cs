using System;
using System.Windows;
using Laevo.ViewModel.Activity.LinkedActivity;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityBorderThicknessConverter : AbstractMultiValueConverter<object, Thickness>
	{
		public override Thickness Convert( object[] values )
		{
			const int thick = 4;
			const int thin = 2;
			const int hide = 0;

			bool isOpen = (bool)values[ 0 ];
			var position = (ActivityPosition)values[ 1 ];

			int normal = isOpen ? thick : thin;
			int left = position.EqualsAny( ActivityPosition.Middle, ActivityPosition.End ) ? hide : normal;
			int right = position.EqualsAny( ActivityPosition.Start, ActivityPosition.Middle ) ? hide : normal;

			return new Thickness( left, normal, right, normal );
		}

		public override object[] ConvertBack( Thickness value )
		{
			throw new NotImplementedException();
		}
	}
}