using System;
using System.Windows;
using Whathecode.System.Windows.Data;

namespace Laevo.View.Common
{
	public class PopupIconMarginConverter : AbstractMultiValueConverter<double, Thickness>
	{
		public Thickness AddMargin { get; set; }


		public override Thickness Convert( double[] values )
		{
			double width = values[ 0 ];
			double height = values[ 1 ];

			return new Thickness(
				(-width / 2) - AddMargin.Left,
				(-height / 2) - AddMargin.Top,
				0, 0 );
		}

		public override double[] ConvertBack( Thickness value )
		{
			throw new NotSupportedException();
		}
	}
}
