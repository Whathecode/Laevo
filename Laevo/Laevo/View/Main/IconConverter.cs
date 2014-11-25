using System;
using System.Windows.Media;
using Whathecode.System.Windows.Data;


namespace Laevo.View.Main
{
	class IconConverter : AbstractValueConverter<int, ImageSource>
	{
		readonly ImageSource _normal;
		readonly ImageSource _interruptions;

		public IconConverter()
		{
			const string iconPath = "pack://application:,,,/Laevo;component/View/Main/";
			_normal = new ImageSourceConverter().ConvertFromString( iconPath + "LaevoTray.ico" ) as ImageSource;
			_interruptions = new ImageSourceConverter().ConvertFromString( iconPath + "LaevoTrayInterruption.ico" ) as ImageSource;
		}

		public override ImageSource Convert( int value )
		{
			if ( value > 0 )
			{
				return _interruptions;
			}
			else
			{
				return _normal;
			}
		}

		public override int ConvertBack( ImageSource value )
		{
			throw new NotSupportedException();
		}
	}
}
