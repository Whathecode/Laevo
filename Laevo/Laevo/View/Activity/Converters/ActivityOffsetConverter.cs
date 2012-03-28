﻿using System;
using System.Globalization;
using System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	public class ActivityOffsetConverter : IMultiValueConverter
	{
		// TODO: Get these offsets from somewhere else instead.
		const double TopOffset = 90;
		const double BottomOffset = 45;

		double _containerHeight;
		double _heightPercentage;
		double _availableHeight;
		double _offsetPercentage;


		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			_offsetPercentage = (double)values[ 0 ];
			_containerHeight = (double)values[ 1 ];
			_availableHeight = _containerHeight - TopOffset - BottomOffset;
			_heightPercentage = (double)values[ 2 ];
			_availableHeight -= _heightPercentage * _availableHeight;

			return (_availableHeight * _offsetPercentage) + BottomOffset;
		}

		public object[] ConvertBack( object offset, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			double offsetPercentage = ((double)offset - BottomOffset) / _availableHeight;

			return new []
			{
				offsetPercentage != _offsetPercentage ? offsetPercentage : Binding.DoNothing,
				Binding.DoNothing,
				Binding.DoNothing
			};
		}
	}
}
