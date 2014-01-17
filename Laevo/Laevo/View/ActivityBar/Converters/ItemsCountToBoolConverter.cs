using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Whathecode.System.Windows.Data;


namespace Laevo.View.ActivityBar.Converters
{
	public class ItemsCountToBoolConverter : AbstractValueConverter<int, Visibility>
	{
		public override Visibility Convert( int value )
		{
			return value > 1 ? Visibility.Visible : Visibility.Hidden;
		}

		public override int ConvertBack( Visibility value )
		{
			throw new NotImplementedException();
		}
	}
}
