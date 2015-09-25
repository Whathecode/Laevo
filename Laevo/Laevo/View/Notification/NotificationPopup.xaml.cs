using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using Laevo.ViewModel.Notification;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Timer = System.Timers.Timer;


namespace Laevo.View.Notification
{
	/// <summary>
	/// Interaction logic for Notification.xaml
	/// </summary>
	public partial class NotificationPopup
	{
		readonly double _workingAreaHeight = Screen.PrimaryScreen.WorkingArea.Height;
		readonly double _workingAreaWidth = Screen.PrimaryScreen.WorkingArea.Width;
		Timer _timer;
		Storyboard _storyboard;

		Action _showAnimation;
		Action _hideAnimation;

		public NotificationPopup()
		{
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.Manual;
		}

		void OnLoaded( object sender, RoutedEventArgs e )
		{
			var dataContext = (NotificationViewModel)DataContext;
			var animation = dataContext.Animation;

			if ( animation == AnimationType.Slide )
			{
				_showAnimation = () => PerformAnimation( LeftProperty, _workingAreaWidth, _workingAreaWidth - ActualWidth, TimeSpan.FromSeconds( 1 ) );
				_hideAnimation = () => PerformAnimation( OpacityProperty, 1, 0, TimeSpan.FromSeconds( 1 ), dataContext.Dissmiss );
			}
			else if ( animation == AnimationType.Fade )
			{
				_showAnimation = () => PerformAnimation( OpacityProperty, 0, 1, TimeSpan.FromSeconds( 1 ) );
				_hideAnimation = () => PerformAnimation( OpacityProperty, 1, 0, TimeSpan.FromSeconds( 1 ), dataContext.Dissmiss );
			}

			// Hack, since it is impossible to bind to the "To" and "From" properties logic has to be handled here.
			// DataContext index is used for queuing the notifications in a column. 
			Top = _workingAreaHeight - ActualHeight -  ( dataContext.Index - 1  ) * ActualHeight;
			Left = _workingAreaWidth - Width;

			ShowNotification();
			HideDelayed( 5000 );
		}

		void ShowNotification()
		{
			if (_showAnimation != null)
				_showAnimation();
		}

		public void HideNotification()
		{
			if (_hideAnimation != null)
				_hideAnimation();
		}

		void HideDelayed( int miliseconds )
		{
			_timer = new Timer
			{
				AutoReset = false,
				Interval = miliseconds
			};
			_timer.Elapsed += ( sender, args ) =>
			{
				Dispatcher.BeginInvoke( new Action( HideNotification ) );
				_timer.Stop();
			};
			_timer.Start();
		}

		void PerformAnimation( DependencyProperty animationProperty, double from, double to, TimeSpan duration, Action postAction = null )
		{
			var animation = new DoubleAnimation
			{
				From = from,
				To = to,
				Duration = duration,
			};
			Storyboard.SetTarget( animation, Control );
			Storyboard.SetTargetProperty( animation, new PropertyPath( animationProperty ) );

			_storyboard = new Storyboard();
			_storyboard.Children.Add( animation );
			if ( postAction != null )
			{
				_storyboard.Completed += ( sender, args ) => postAction();
			}
			_storyboard.Begin();
		}

		void OnMouseEnter( object sender, MouseEventArgs e )
		{
			_timer.Stop();
			if ( _storyboard != null )
			{
				_storyboard.Stop();
			}
		}
	}
}