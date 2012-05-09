﻿using System;
using Whathecode.System.Windows.Data;
using Laevo.ViewModel.ActivityOverview.Binding;


namespace Laevo.View.ActivityOverview.Converters
{
	public class ActivityModeToTopMostConverter : AbstractGenericValueConverter<Mode, bool>
	{
		public override bool Convert( Mode value )
		{
			return value == Mode.Edit ? false : true;
		}

		public override Mode ConvertBack( bool value )
		{
			throw new NotSupportedException();
		}
	}
}
