using System.Windows;
using System.Windows.Input;


namespace Laevo.View.Notification
{
	/// <summary>
	/// Interaction logic for NotificationButtons.xaml
	/// </summary>
	public partial class NotificationButtons
	{
		public NotificationButtons()
		{
			InitializeComponent();
		}

		void SetFocus( object sender, MouseButtonEventArgs e )
		{
			( (UIElement)e.Source ).Focus();
		}
	}
}