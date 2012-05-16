using System;
using System.Windows;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class AttentionLinesVisibilityConverter : AbstractGenericValueConverter<bool, Visibility>
	{
		public override Visibility Convert( bool value )
		{
			return value ? Visibility.Visible : Visibility.Collapsed;
		}

		public override bool ConvertBack( Visibility value )
		{
			throw new NotSupportedException();
		}
	}
}
