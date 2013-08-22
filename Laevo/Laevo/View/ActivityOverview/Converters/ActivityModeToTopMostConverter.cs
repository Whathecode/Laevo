using System;
using Laevo.ViewModel.ActivityOverview;
using Whathecode.System.Windows.Data;
using Laevo.ViewModel.ActivityOverview.Binding;


namespace Laevo.View.ActivityOverview.Converters
{
	public class ActivityModeToTopMostConverter : AbstractValueConverter<Mode, bool>
	{
		public override bool Convert( Mode value )
		{
			return value != Mode.Edit;
		}

		public override Mode ConvertBack( bool value )
		{
			throw new NotSupportedException();
		}
	}
}
