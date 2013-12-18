using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;


namespace Laevo.View.Activity
{
	[StructLayout( LayoutKind.Sequential )]
	public struct Margins
	{
		public int left;
		public int right;
		public int top;
		public int bottom;
	};

	public class GlassEffect
	{
		[DllImport("dwmapi.dll", PreserveSig = false)]
		public static extern bool DwmIsCompositionEnabled();

		[DllImport( "DwmApi.dll" )]
		public static extern int DwmExtendFrameIntoClientArea( IntPtr hwnd, ref Margins pMarInset );

		/// <summary>
		/// Registers new dependency property which allows to use Windows aero styling in specific windows by setting
		/// GlassEffect.IsEnabled to true.
		/// </summary>
		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.RegisterAttached( "IsEnabled",
				typeof( Boolean ),
				typeof( GlassEffect ),
				new FrameworkPropertyMetadata( OnIsEnabledChanged ) );

		public static void SetIsEnabled( DependencyObject element, Boolean value )
		{
			element.SetValue( IsEnabledProperty, value );
		}

		public static Boolean GetIsEnabled( DependencyObject element )
		{
			return (Boolean)element.GetValue( IsEnabledProperty );
		}

		public static void OnIsEnabledChanged( DependencyObject obj, DependencyPropertyChangedEventArgs args )
		{
			if ( (bool)args.NewValue != true ) return;

			var window = (Window)obj;
			window.Loaded += WindowLoaded;
		}

		/// <summary>
		/// Applies Windows aero styling to specific window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		static void WindowLoaded( object sender, RoutedEventArgs e )
		{
			// Checks if Aero theme is turned on. 
			if ( Environment.OSVersion.Version.Major >= 6 && DwmIsCompositionEnabled() )
			{
				var window = (Window)sender;
				var originalBackground = window.Background;
				window.Background = Brushes.Transparent;
				try
				{
					var mainWindowPointer = new WindowInteropHelper( window ).Handle;
					var mainWindowsSource = HwndSource.FromHwnd( mainWindowPointer );
					if ( mainWindowsSource != null )
					{
						if ( mainWindowsSource.CompositionTarget != null )
						{
							mainWindowsSource.CompositionTarget.BackgroundColor = Color.FromArgb( 0, 0, 0, 0 );
						}
						var margins = new Margins { left = -1, right = -1, top = -1, bottom = -1 };

						DwmExtendFrameIntoClientArea( mainWindowsSource.Handle, ref margins );
					}
				}
				catch ( DllNotFoundException )
				{
					window.Background = originalBackground;
				}
			}
			else
			{
				// TODO: Disable resize mode to hide a window border when aero theme is not used in order to hace better styling.
				//window.ResizeMode = ResizeMode.NoResize;
			}
		}
	}
}