using System;
using System.Windows.Media;
using Laevo.ViewModel.Activity;
using Whathecode.System.Windows.Data;


namespace Laevo.View.ActivityBar
{
	class ActivityBackgroundConverter : AbstractMultiValueConverter<object, Brush>
	{
		public override Brush Convert( object[] values )
		{
			var activity = (ActivityViewModel)values[ 0 ];
			var color = (Color)values[ 1 ];
			var selectedActivity = (ActivityViewModel)values[ 2 ];

			if ( activity == selectedActivity )
			{
				return new LinearGradientBrush( Colors.White, Colors.Yellow, 90 );
			}
			else
			{
				return new SolidColorBrush( color );
			}
		}

		public override object[] ConvertBack( Brush value )
		{
			throw new NotSupportedException();
		}
	}
}
