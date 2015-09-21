
using System;


namespace Laevo.View.ActivityOverview
{
	/// <summary>
	/// Interaction logic for NotificationsMenu.xaml
	/// </summary>
	public partial class NotificationList
	{
		public NotificationList()
		{
			InitializeComponent();
		}

		void OnDeactivated( object sender, EventArgs e )
		{
			Hide();
		}
	}
}
