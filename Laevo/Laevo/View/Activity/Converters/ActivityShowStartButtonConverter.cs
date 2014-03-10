using System;
using Laevo.ViewModel.Activity.LinkedActivity;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class ActivityShowStartButtonConverter : AbstractValueConverter<ActivityPosition, bool>
	{
		public override bool Convert( ActivityPosition value )
		{
			return value == ActivityPosition.None || value == ActivityPosition.End;
		}

		public override ActivityPosition ConvertBack( bool value )
		{
			throw new NotSupportedException();
		}
	}
}