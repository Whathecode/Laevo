using System;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Activity.Converters
{
	class OccuranceEndConverter : AbstractMultiValueConverter<object, DateTime>
	{
		DateTime _occurance;
		public override DateTime Convert( object[] values )
		{
			_occurance = (DateTime)values[ 0 ];
			var duration = (TimeSpan)values[ 1 ];

			return _occurance + duration;
		}

		public override object[] ConvertBack( DateTime value )
		{
			if ( value <= _occurance )
			{
				return new object[]
				{
					_occurance,
					TimeSpan.FromMinutes( 0 )
				};
			}
			else
			{
				return new object[]
				{
					_occurance,
					value - _occurance
				};
			}
		}
	}
}
