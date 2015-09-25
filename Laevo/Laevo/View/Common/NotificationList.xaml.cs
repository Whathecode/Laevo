using System;
using System.Collections.ObjectModel;
using System.Windows;
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