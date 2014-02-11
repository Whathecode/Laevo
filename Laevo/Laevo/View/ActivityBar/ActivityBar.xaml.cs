using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using Whathecode.System.Extensions;


namespace Laevo.View.ActivityBar
{
	/// <summary>
	/// Interaction logic for ActivityBar.xaml
	/// </summary>
	public partial class ActivityBar
	{
		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		public static extern IntPtr DefWindowProc( IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam );

		const int WindowsHitTest = 0x0084;
		const int HitBorder = 18;
		const int HitBottomBorder = 15;
		const int HitBottomleftBorderCorner = 16;
		const int HitBottomRightBorderCorner = 17;
		const int HitLeftBorder = 10;
		const int HitRightBorder = 11;
		const int HitTopBorder = 12;
		const int HitTopLeftBorderCorner = 13;
		const int HitTopRightBorderCorner = 14;

		const string ActivityButtonName = "ActivityButton";
		const double TopWhenVisible = -5;

		readonly TimeSpan _displayTime = TimeSpan.FromSeconds( 4 );
		readonly DoubleAnimation _hideAnimation;

		bool _barGotClosed;

		// How much should we shift Activity menu to display it in left corner bar position. Values are relative to menu owner- ActivityButton.
		const int MenuHorizontalShift = -16;
		const int MenuVerticalShift = 10;

		public ActivityBar()
		{
			InitializeComponent();

			ResizeToScreenWidth();
			SystemEvents.DisplaySettingsChanged += ( s, a ) => ResizeToScreenWidth();

			Loaded += OnLoaded;
			Deactivated += OnDeactivated;

			Activated += ( s, args ) => PinTaskbar();

			// Create animation which hides the activity bar, sliding it out of view.
			_hideAnimation = new DoubleAnimation
			{
				From = TopWhenVisible,
				To = -( Height + 5 ),
				Duration = new Duration( TimeSpan.FromSeconds( 0.5 ) ),
				FillBehavior = FillBehavior.Stop,
				BeginTime = _displayTime
			};
		}


		/// <summary>
		/// Position window so that the borders aren't visible and it looks like the taskbar.
		/// </summary>
		void ResizeToScreenWidth()
		{
			// Sets width of Activity Bar to half of the screen and places it in the middle.
			Width = SystemParameters.PrimaryScreenWidth / 2;
			Left = ( SystemParameters.PrimaryScreenWidth / 2 ) - ( Width / 2 );

			// Place activity menu in bottom left corner of Activity Bar.
			ContextMenu.HorizontalOffset = MenuHorizontalShift;
			ContextMenu.VerticalOffset = MenuVerticalShift;
		}

		void OnLoaded( object sender, RoutedEventArgs e )
		{
			// Disable default resize behavior by overriding default events.
			var mainWindowPointer = new WindowInteropHelper( this ).Handle;
			var mainWindowSource = HwndSource.FromHwnd( mainWindowPointer );
			if ( mainWindowSource != null )
			{
				mainWindowSource.AddHook( HandleWindowHits );
			}
		}

		/// <summary>
		/// Override the window hit test. If the cursor is over a resize border, return a standard border result instead.
		/// </summary>
		IntPtr HandleWindowHits( IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled )
		{
			if ( message != WindowsHitTest )
			{
				return IntPtr.Zero;
			}

			handled = true;
			var hitLocation = DefWindowProc( hwnd, message, wParam, lParam ).ToInt32();
			switch ( hitLocation )
			{
				case HitBottomBorder:
				case HitBottomleftBorderCorner:
				case HitBottomRightBorderCorner:
				case HitLeftBorder:
				case HitRightBorder:
				case HitTopBorder:
				case HitTopLeftBorderCorner:
				case HitTopRightBorderCorner:
					hitLocation = HitBorder;
					break;
			}

			return new IntPtr( hitLocation );
		}

		/// <summary>
		/// Show the activity bar and hide it after some time.
		/// </summary>
		public void ShowActivityBar( bool activate )
		{
			ShowBarFor( _displayTime );

			if ( activate )
			{
				Activate();
				ActivityName.Select( 0, ActivityName.Text.Length );
				ActivityName.Focus();
			}
		}

		void ShowBarFor( TimeSpan delay )
		{
			PinTaskbar();

			// Hide the activity bar after some time if it's not activated.
			if ( !IsInUse() )
			{
				_hideAnimation.BeginTime = delay;
				BeginAnimation( TopProperty, _hideAnimation );
			}
		}

		public void PinTaskbar()
		{
			_hideAnimation.Completed -= HideCompleted;
			BeginAnimation( TopProperty, null );
			Top = TopWhenVisible;
			_hideAnimation.Completed += HideCompleted;

			Show();
		}

		/// <summary>
		/// Determines whether or not the activity bar is currently in use.
		/// </summary>
		public bool IsInUse()
		{
			return IsActive && !_barGotClosed;
		}

		void HideCompleted( object sender, EventArgs e )
		{
			Hide(); // Hide the window entirely since it is no longer visible.
		}

		void OnDeactivated( object sender, EventArgs e )
		{
			// Force activity name binding to update.
			var nameBinding = ActivityName.GetBindingExpression( TextBox.TextProperty );
			if ( nameBinding != null && !ActivityName.IsReadOnly && ActivityName.IsEnabled )
			{
				nameBinding.UpdateSource();
			}

			// Hide the infobox.
			if ( !_barGotClosed )
			{
				_hideAnimation.BeginTime = TimeSpan.Zero;
				BeginAnimation( TopProperty, _hideAnimation );
			}
			_barGotClosed = false;
		}

		void LabelKeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key.EqualsAny( Key.Enter, Key.Escape ) )
			{
				_barGotClosed = true;

				// Move the focus from name textbox after user ends name editing to finnish action.
				// Without this feature textbox will be focused during next Activity bar call.
				PassFocusToPreviousItem();

				ShowBarFor( TimeSpan.Zero );
			}
		}

		/// <summary>
		/// Passes the focus to previous visual item.
		/// </summary>
		internal void PassFocusToPreviousItem()
		{
			ActivityName.MoveFocus( new TraversalRequest( FocusNavigationDirection.Previous ) );
		}

		/// <summary>
		/// Switches between current and opened activities.
		/// </summary>
		internal void SelectNextActivity( int selectionIndex )
		{
			Activate();
			//_barGotClosed = false;\
			PinTaskbar();

			// Move focus to next activity button.
			var contentPresenter = (ContentPresenter)ItemsControlActivities.ItemContainerGenerator.ContainerFromIndex( selectionIndex );
			var activityButton = (Button)contentPresenter.ContentTemplate.FindName( ActivityButtonName, contentPresenter );
			activityButton.Focus();
		}

		/// <summary>
		/// Resets properties and shows Activity Menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void ShowActivityMenu( object sender, RoutedEventArgs e )
		{
			ActivityButton.ContextMenu.Visibility = Visibility.Visible;
			ActivityButton.ContextMenu.Focus();
			ActivityButton.ContextMenu.IsEnabled = true;
			ActivityButton.ContextMenu.PlacementTarget = ActivityButton;
			ActivityButton.ContextMenu.Placement = PlacementMode.Bottom;
			ActivityButton.ContextMenu.IsOpen = true;
		}

		/// <summary>
		/// Hides Activity Menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void HideActivityMenu( object sender, MouseButtonEventArgs e )
		{
			ActivityButton.ContextMenu.Visibility = Visibility.Hidden;
			ActivityButton.ContextMenu.IsOpen = false;
		}
	}
}