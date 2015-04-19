using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Laevo.View.Common
{
	class LaevoPopup : ContentControl
	{
		public static readonly DependencyProperty PopupImageProperty = DependencyProperty.Register(
			"PopupImage", typeof( ImageSource ),
			typeof( LaevoPopup ) );
		public ImageSource PopupImage
		{
			get { return GetValue( PopupImageProperty ) as ImageSource; }
			set { SetValue( PopupImageProperty, value ); }
		}
	}
}