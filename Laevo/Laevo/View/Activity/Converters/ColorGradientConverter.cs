using System;
using System.Windows.Media;
using Whathecode.System.Windows.Data;
using Whathecode.System.Windows.Media.Extensions;


namespace Laevo.View.Activity.Converters
{
	class ColorGradientConverter : AbstractValueConverter<Color, Color>
	{
		public override Color Convert( Color value )
		{
			return value.Darken( 0.2 );
		}

		public override Color ConvertBack( Color value )
		{
			throw new NotSupportedException();
		}
	}
}
