using System;
using System.Windows.Media;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class ActivityBorderConverter : AbstractGenericValueConverter<bool, Brush>
	{
		public override Brush Convert( bool value )
		{
			return new SolidColorBrush( value ? Colors.Yellow : Colors.White );
		}

		public override bool ConvertBack( Brush value )
		{
			throw new NotSupportedException();
		}
	}
}
