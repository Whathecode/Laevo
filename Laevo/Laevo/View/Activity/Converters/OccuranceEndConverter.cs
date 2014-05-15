using System;
using System.Windows;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class OccuranceEndConverter : AbstractMultiValueConverter<object, DateTime>
	{
		DateTime _occurance;


		public override DateTime Convert( object[] values )
		{
			// TODO: For now we need to check for unset values since edit popup has a data context of LinkedActivityViewModel which is not present when it is a todo item.
			if ( values[ 0 ] == DependencyProperty.UnsetValue || values[ 1 ] == DependencyProperty.UnsetValue )
			{
				return DateTime.Now;
			}
			
			_occurance = (DateTime)values[ 0 ];
			var duration = (TimeSpan)values[ 1 ];
			return _occurance + duration;
		}

		public override object[] ConvertBack( DateTime value )
		{
			TimeSpan duration = value <= _occurance
				? TimeSpan.FromMinutes( 0 )
				: value - _occurance;

			return new object[]
			{
				_occurance,
				duration
			};
		}
	}
}