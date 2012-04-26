using System;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class ActivityBorderConverter : AbstractGenericValueConverter<bool, double>
	{
		public override double Convert( bool value )
		{
			return value ? 2 : 0;
		}

		public override bool ConvertBack( double value )
		{
			throw new NotSupportedException();
		}
	}
}
