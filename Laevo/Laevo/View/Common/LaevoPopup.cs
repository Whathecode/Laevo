using System.Windows;
using System.Windows.Media;


namespace Laevo.View.Common
{
	/// <summary>
	/// Interaction logic for LaevoPopup2.xaml
	/// </summary>
	public class LaevoPopup : Window
	{
		public static readonly DependencyProperty PopupImageProperty = DependencyProperty.Register(
			"PopupImage", typeof( ImageSource ),
			typeof( LaevoPopup ) );

		public ImageSource PopupImage
		{
			get { return GetValue( PopupImageProperty ) as ImageSource; }
			set { SetValue( PopupImageProperty, value ); }
		}


		public LaevoPopup()
		{
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Style = Application.Current.FindResource( typeof( LaevoPopup ) ) as Style;
		}
	}
}
