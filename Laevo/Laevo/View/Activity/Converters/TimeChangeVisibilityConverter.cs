using System;
using System.Windows;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class TimeChangeVisibilityConverter : AbstractValueConverter<DateTime, Visibility>
	{
		public override Visibility Convert( DateTime value )
		{
			DateTime occurence = value;
			return occurence > DateTime.Now ? Visibility.Visible : Visibility.Collapsed;
		}

		public override DateTime ConvertBack( Visibility value )
		{
			throw new NotSupportedException();
		}
	}
}
