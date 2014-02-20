using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;


namespace Laevo.View.ActivityBar
{
	// TODO: Move class to FCL or common dir, maybe? 

	public class DisableResize
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

		/// <summary>
		/// Registers new dependency property which allows to disable resize feature in a window by setting
		/// DisableResize.IsDisabled to true.
		/// </summary>
		public static readonly DependencyProperty IsDisabledProperty =
			DependencyProperty.RegisterAttached( "IsDisabled",
				typeof( Boolean ),
				typeof( DisableResize ),
				new FrameworkPropertyMetadata( OnIsDisabledChanged ) );

		public static void SetIsDisabled( DependencyObject element, Boolean value )
		{
			element.SetValue( IsDisabledProperty, value );
		}

		public static Boolean GetIsDisabled( DependencyObject element )
		{
			return (Boolean)element.GetValue( IsDisabledProperty );
		}

		public static void OnIsDisabledChanged( DependencyObject obj, DependencyPropertyChangedEventArgs args )
		{
			if ( !(bool)args.NewValue ) return;

			var window = (Window)obj;
			window.Loaded += OnLoaded;
		}

		static void OnLoaded( object sender, RoutedEventArgs e )
		{
			// Disable default resize behavior by overriding default events.
			var mainWindowPointer = new WindowInteropHelper( (Window)sender ).Handle;
			var mainWindowSource = HwndSource.FromHwnd( mainWindowPointer );
			if ( mainWindowSource != null )
			{
				mainWindowSource.AddHook( HandleWindowHits );
			}
		}

        /// <summary>
		/// Override the window hit test. If the cursor is over a resize border, return a standard border result instead.
		/// </summary>
		public static IntPtr HandleWindowHits( IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled )
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
    }
}
