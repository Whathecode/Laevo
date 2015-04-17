using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Laevo.View.Common
{
	class LaevoPopup : ContentControl
	{

		public ImageSource PopupImage
		{
			get { return GetValue( PopupImageProperty ) as ImageSource; }
			set { SetValue( PopupImageProperty, value ); }
		}

		public static readonly DependencyProperty PopupImageProperty =
			DependencyProperty.Register( "PopupImage", typeof( ImageSource ), typeof( LaevoPopup ) );

		public Color PopupColor
		{
			get { return (Color)GetValue( PopupColorProperty ); }
			set { SetValue( PopupColorProperty, value ); }
		}

		public static readonly DependencyProperty PopupColorProperty = DependencyProperty.Register( "PopupColor", typeof( Color ), typeof( LaevoPopup ) );
	}
}