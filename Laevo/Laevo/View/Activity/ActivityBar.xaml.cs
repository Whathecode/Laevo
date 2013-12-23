using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using Whathecode.System.Extensions;
using TextBox = System.Windows.Controls.TextBox;


namespace Laevo.View.Activity
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

		[DllImport( "dwmapi.dll", EntryPoint = "#127" )]
		static extern void DwmGetColorizationParameters( out Dwmcolorizationparams parameters );
		public struct Dwmcolorizationparams
		{
			public uint
				ColorizationColor,
				ColorizationAfterglow,
				ColorizationColorBalance,
				ColorizationAfterglowBalance,
				ColorizationBlurBalance,
				ColorizationGlassReflectionIntensity,
				ColorizationOpaqueBlend;
		}

		const double TopWhenVisible = -3;
		readonly TimeSpan _displayTime = TimeSpan.FromSeconds( 4 );
		readonly DoubleAnimation _hideAnimation;


		public ActivityBar()
		{
			InitializeComponent();

			ResizeToScreenWidth();
			SystemEvents.DisplaySettingsChanged += ( s, a ) => ResizeToScreenWidth();

			Loaded += OnLoaded;
			Deactivated += OnDeactivated;

			// TODO: The activated event is the more general event we need, but due to a bug in the VDM which sometimes activates the wrong windows this sometimes causes issues.
			PreviewMouseDown += ( s, args ) => PinTaskbar();
			//Activated += ( s, args ) => PinTaskbar();

			// Create animation which hides the activity bar, sliding it out of view.
			_hideAnimation = new DoubleAnimation
			{
				From = TopWhenVisible,
				To = -(Height + 5),
				Duration = new Duration( TimeSpan.FromSeconds( 1 ) ),
				FillBehavior = FillBehavior.Stop,
				BeginTime = _displayTime
			};
		}


		/// <summary>
		/// Position window so that the borders aren't visible and it looks like the taskbar.
		/// </summary>
		void ResizeToScreenWidth()
		{
			Left = -5;
			Width = SystemParameters.PrimaryScreenWidth + 10;
		}

		/// <summary>
		/// Gets a color from windows registry in order to apply it to a window in both Aero and othere themes. (Not used for now)
		/// </summary>
		Color GetWindowColorizationColor( bool opaque )
		{
			Dwmcolorizationparams windowsColors;
			DwmGetColorizationParameters( out windowsColors );

			return Color.FromArgb(
				(byte)( opaque ? 255 : windowsColors.ColorizationColor >> 24 ),
				(byte)( windowsColors.ColorizationColor >> 16 ),
				(byte)( windowsColors.ColorizationColor >> 8 ),
				(byte)windowsColors.ColorizationColor );
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
		static IntPtr HandleWindowHits( IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled )
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

			_hideAnimation.BeginTime = delay;
			BeginAnimation( TopProperty, _hideAnimation );
		}

		void PinTaskbar()
		{
			Top = TopWhenVisible;
			Show();

			_hideAnimation.Completed -= HideCompleted;
			BeginAnimation( TopProperty, null );
			Top = TopWhenVisible;
			_hideAnimation.Completed += HideCompleted;
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
				ShowBarFor( TimeSpan.Zero );
			}
			_barGotClosed = false;
		}

		bool _barGotClosed;
		void LabelKeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key.EqualsAny( Key.Enter, Key.Escape ) )
			{
				_barGotClosed = true;
				ShowBarFor( TimeSpan.Zero );
			}
		}
	}
}