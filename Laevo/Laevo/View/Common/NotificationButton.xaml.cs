﻿using System.Windows;
using System.Windows.Input;
using System.Windows.Media;


namespace Laevo.View.Common
{
	/// <summary>
	/// Interaction logic for NotificationButton.xaml
	/// </summary>
	public partial class NotificationButton
	{
		public static readonly DependencyProperty NotificationsCountProperty = DependencyProperty.Register(
			"NotificationsCount", typeof( int ),
			typeof( NotificationButton ) );

		public static readonly DependencyProperty ButtonImageProperty = DependencyProperty.Register(
			"ButtonImage", typeof( ImageSource ),
			typeof( NotificationButton ) );

		public static readonly DependencyProperty OpenNotificationsCommandProperty = DependencyProperty.Register(
			"OpenNotificationsCommand", typeof( ICommand ),
			typeof( NotificationButton ) );

		public static readonly DependencyProperty ButtonStyleProperty = DependencyProperty.Register(
			"ButtonStyle", typeof( Style ),
			typeof( NotificationButton ) );

		public int NotificationsCount
		{
			get { return (int)GetValue( NotificationsCountProperty ); }
			set { SetValue( NotificationsCountProperty, value ); }
		}

		public ImageSource ButtonImage
		{
			get { return GetValue( ButtonImageProperty ) as ImageSource; }
			set { SetValue( ButtonImageProperty, value ); }
		}

		public ICommand OpenNotificationsCommand
		{
			get { return GetValue( OpenNotificationsCommandProperty ) as ICommand; }
			set { SetValue( OpenNotificationsCommandProperty, value ); }
		}

		public Style ButtonStyle
		{
			get { return GetValue( ButtonStyleProperty ) as Style; }
			set { SetValue( ButtonStyleProperty, value ); }
		}

		public NotificationButton()
		{
			InitializeComponent();
		}
	}
}