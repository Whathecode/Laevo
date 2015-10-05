using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
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
		public enum NotificationState
		{
			Hidden,
			Shown
		}

		readonly double _workingAreaHeight = Screen.PrimaryScreen.WorkingArea.Height;
		readonly double _workingAreaWidth = Screen.PrimaryScreen.WorkingArea.Width;

		readonly Timer _hideTimer;
		Storyboard _storyboard;

		int _popupRowIndex;

		readonly Action _stackAnimation;

		NotificationState _state;

		bool _stacked;
		readonly double _hide;
		readonly double _hover;
		ImportanceLevel _importanceLevel;

		public NotificationPopup()
		{
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.Manual;

			_storyboard = new Storyboard();

			_hideTimer = new Timer
			{
				AutoReset = false
			};
			_hideTimer.Elapsed += ( sender, args ) =>
			{
				Dispatcher.BeginInvoke( new Action( () =>
				{
					// Hide partially.
					PerformAnimation( LeftProperty, _workingAreaWidth - ActualWidth, _workingAreaWidth - _hide * ActualWidth,
						TimeSpan.FromSeconds( 1 ),
						_stacked ? null : _stackAnimation );

					_state = NotificationState.Hidden;
				} ) );
				_hideTimer.Stop();
			};

			// Value indicating how big portion a notification pop-up is shown during the hidden state.
			_hide = 0.2;
			// Value indicating how big portion a notification pop-up is shown during the hidden-hovered state.
			_hover = 0.3;

			_stackAnimation = () => PerformAnimation( TopProperty, Top, Top + ( _popupRowIndex - 1 ) * ActualHeight * 0.8, TimeSpan.FromSeconds( 1 ),
				() => _stacked = true );
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

			_storyboard.Stop();
			_storyboard = new Storyboard();
			_storyboard.Children.Add( animation );
			if ( postAction != null )
			{
				_storyboard.Completed += ( sender, args ) => postAction();
			}
			_storyboard.Begin();
		}

		void OnLoaded( object sender, RoutedEventArgs e )
		{
			var dataContext = (NotificationViewModel)DataContext;
			_importanceLevel = dataContext.ImportanceLevel;
			// DataContext index is used for queuing the notifications in a column.
			_popupRowIndex = dataContext.Index;

			// Hack, since it is impossible to bind to the "To" and "From" properties, setting the initial position of the pop-up handled here.
			Top = _workingAreaHeight - ActualHeight - ( _popupRowIndex - 1 ) * ActualHeight;
			Left = _workingAreaWidth - Width;

			if ( _importanceLevel == ImportanceLevel.High )
			{
				_state = NotificationState.Shown;

				// Full show and delayed hide.
				PerformAnimation( OpacityProperty, 0, 1, TimeSpan.FromSeconds( 1 ) );
				HideDelayed( 5000 );
			}
			else if ( _importanceLevel == ImportanceLevel.Low )
			{
				_state = NotificationState.Hidden;

				// Show partially.
				PerformAnimation( LeftProperty, _workingAreaWidth, _workingAreaWidth - _hide * ActualWidth, TimeSpan.FromSeconds( 1 ), _stackAnimation );
			}
		}

		void HideDelayed( int miliseconds )
		{
			_hideTimer.Stop();
			_hideTimer.Interval = miliseconds;
			_hideTimer.Start();
		}

		void OnMouseEnter( object sender, MouseEventArgs e )
		{
			_hideTimer.Stop();

			if ( _state == NotificationState.Hidden )
			{
				// Move left.
				PerformAnimation( LeftProperty, _workingAreaWidth - _hide * ActualWidth, _workingAreaWidth - ActualWidth * _hover, TimeSpan.FromSeconds( 0.5 ) );
			}
		}

		void OnMouseLeave( object sender, MouseEventArgs e )
		{
			if ( _state == NotificationState.Hidden )
			{
				// Move right.
				PerformAnimation( LeftProperty, _workingAreaWidth - _hover * ActualWidth, _workingAreaWidth - ActualWidth * _hide,
					TimeSpan.FromSeconds( 0.5 ) );
			}
			else if ( _state == NotificationState.Shown )
			{
				HideDelayed( 5000 );
			}
		}

		void OnMouseDown( object sender, MouseButtonEventArgs e )
		{
			_state = NotificationState.Shown;
			// Show.
			PerformAnimation( LeftProperty, Left, _workingAreaWidth - ActualWidth, TimeSpan.FromSeconds( 0.1 ) );
		}
	}
}