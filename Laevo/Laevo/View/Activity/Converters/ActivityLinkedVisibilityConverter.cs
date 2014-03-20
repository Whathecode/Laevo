using System;
using System.Windows;
using Laevo.ViewModel.Activity.LinkedActivity;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityLinkedVisibilityConverter : AbstractValueConverter<ActivityPosition, Visibility>
	{
		public override Visibility Convert( ActivityPosition value )
		{
			switch ( value )
			{
				case ActivityPosition.None:
					return Visibility.Collapsed;
				case ActivityPosition.Start:
					return Visibility.Visible;
				case ActivityPosition.Middle:
					return Visibility.Visible;
				case ActivityPosition.End:
					return Visibility.Collapsed;
				default:
					return Visibility.Collapsed;
			}
		}

		public override ActivityPosition ConvertBack( Visibility visibility )
		{
			throw new NotSupportedException();
		}
	}
}