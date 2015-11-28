using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Forms;
using Laevo.ViewModel.Notification;


namespace Laevo.View.Common
{
	/// <summary>
	/// Interaction logic for NotificationList.xaml
	/// </summary>
	public partial class NotificationList
	{
		public static readonly DependencyProperty NotificationsProperty = DependencyProperty.Register(
			"Notifications", typeof( ObservableCollection<NotificationViewModel> ),
			typeof( NotificationList ) );

		public ObservableCollection<NotificationViewModel> Notifications
		{
			get { return GetValue( NotificationsProperty ) as ObservableCollection<NotificationViewModel>; }
			set { SetValue( NotificationsProperty, value ); }
		}

		readonly double _workingAreaHeight = Screen.PrimaryScreen.WorkingArea.Height;

		public NotificationList()
		{
			InitializeComponent();
		}

		void OnDeactivated( object sender, EventArgs e )
		{
			Hide();
		}

		void OnLoaded( object sender, RoutedEventArgs e )
		{
			MaxHeight = _workingAreaHeight / 3;

			Notifications.CollectionChanged += ( o, args ) =>
			{
				if ( Notifications != null && Notifications.Count == 0 )
				{
					if ( IsVisible )
						Hide();
				}
			};
		}
	}
}