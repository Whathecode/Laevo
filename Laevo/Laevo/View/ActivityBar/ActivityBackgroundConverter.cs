using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Laevo.ViewModel.Activity;
using Whathecode.System.Windows.Data;


namespace Laevo.View.ActivityBar
{
	class ActivityBackgroundConverter : AbstractMultiValueConverter<object, Brush>
	{
		static readonly Brush HighlightBrush;


		static ActivityBackgroundConverter()
		{
			var gradientsStops = new List<GradientStop>
			{
				// ReSharper disable PossibleNullReferenceException
				new GradientStop( (Color)ColorConverter.ConvertFromString( "#FFE3F4FC" ), 0 ),
				new GradientStop( (Color)ColorConverter.ConvertFromString( "#FFD8EFFC" ), 0.38 ),
				new GradientStop( (Color)ColorConverter.ConvertFromString( "#FFBEE6FD" ), 0.38 ),
				new GradientStop( (Color)ColorConverter.ConvertFromString( "#FFA6D9F4" ), 1 )
			};
			HighlightBrush = new LinearGradientBrush(new GradientStopCollection(gradientsStops), new Point(0, 0), new Point(0, 1));
		}


		public override Brush Convert( object[] values )
		{
			var activity = (ActivityViewModel)values[ 0 ];
			var color = (Color)values[ 1 ];
			var selectedActivity = (ActivityViewModel)values[ 2 ];

			return activity == selectedActivity
				? HighlightBrush
				: new SolidColorBrush( color );
		}

		public override object[] ConvertBack( Brush value )
		{
			throw new NotSupportedException();
		}
	}
}
