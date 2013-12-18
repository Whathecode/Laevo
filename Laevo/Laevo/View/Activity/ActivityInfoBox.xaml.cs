using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Application = System.Windows.Application;


namespace Laevo.View.Activity
{
	/// <summary>
	/// Interaction logic for ActivityInfoBox.xaml
	/// </summary>
	/// 
	public partial class ActivityInfoBox
	{
		readonly TimeSpan _hideTime = TimeSpan.FromSeconds( 4 );

		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		public static extern IntPtr DefWindowProc(
			IntPtr hWnd,
			int msg,
			IntPtr wParam,
			IntPtr lParam );

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

		readonly DoubleAnimation _toUpAnimation;

		public struct Dwmcolorizationparams
		{
			public uint ColorizationColor,
				ColorizationAfterglow,
				ColorizationColorBalance,
				ColorizationAfterglowBalance,
				ColorizationBlurBalance,
				ColorizationGlassReflectionIntensity,
				ColorizationOpaqueBlend;
		}

		public ActivityInfoBox()
		{
			InitializeComponent();
			Loaded += MainWindowLoaded;

			// Adds 10 pixels to ActivityInfoBox width beacuse of initial top and left position outside of the screen.
			Width += 10; // Probably can be done later by using converter.

			//10 pixels are added to a taskbar height because of initial top position of ActivityInfoBox outside of the screen.
			Height = GetTaskBarHeight() + 10; // Probably can be done later by using converter.

			// Set up animation which hides the info box.
			_toUpAnimation = new DoubleAnimation
			{
				From = -3,
				To = -60,
				Duration = new Duration(TimeSpan.FromSeconds(1)),
				FillBehavior = FillBehavior.Stop,
				BeginTime = _hideTime
			};
		}


		/// <summary>
		/// Gets Windows taskbar height or width depending on its position. 
		/// </summary>
		/// <returns></returns>
		private static Int32 GetTaskBarHeight()
		{
			var taskBarOnTopOrBottom = (Screen.PrimaryScreen.WorkingArea.Width == Screen.PrimaryScreen.Bounds.Width);

			if (taskBarOnTopOrBottom)
			{
				return Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height;
			}
			return Screen.PrimaryScreen.Bounds.Width - Screen.PrimaryScreen.WorkingArea.Width;
		}

		/// <summary>
		/// Gets a color from windows registry in order to apply it to a window in both Aero and othere themes. (Not used for now)
		/// </summary>
		/// <param name="opaque"></param>
		/// <returns>Color</returns>
		Color GetWindowColorizationColor( bool opaque )
		{
			Dwmcolorizationparams windowsColors;
			DwmGetColorizationParameters(out windowsColors);

			return Color.FromArgb((byte)(opaque ? 255 : windowsColors.ColorizationColor >> 24),
				(byte)(windowsColors.ColorizationColor >> 16),
				(byte)(windowsColors.ColorizationColor >> 8),
				(byte)windowsColors.ColorizationColor);
		}

		/// <summary>
		/// Adds a hook to a window in order to disable default resize feature.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void MainWindowLoaded( object sender, RoutedEventArgs e )
		{
			try
			{
				// Obtain the window handle for WPF application.
				var mainWindowPointer = new WindowInteropHelper( this ).Handle;
				var mainWindowSource = HwndSource.FromHwnd( mainWindowPointer );
				
				if ( mainWindowSource != null )
				{
					mainWindowSource.AddHook( handleWindowHits );
				}
			}
			// If it is not Windows Vista or higher, paint background white. 
			// If there is a need to support systems older than Vista, good to change.
			catch ( DllNotFoundException )
			{
				Application.Current.MainWindow.Background = Brushes.White;
			}
		}

		IntPtr handleWindowHits( IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled )
		{
			// Override the window hit test. If the cursor is over a resize border,
			// return a standard border result instead.
			if ( message == WindowsHitTest )
			{
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
			return IntPtr.Zero;
		}

		/// <summary>
		/// Places the caret on the last position of the name text box in order to allow user to update the name.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void ActivityNameOnLoadedEventHandler( object sender, RoutedEventArgs e )
		{
			ActivityName.CaretIndex = ActivityName.Text.Length;
		}

		/// <summary>
		/// Resets animation responsible for Activity Info Box slidning up.
		/// </summary>
		void ResetToUpAnimation()
		{
			_toUpAnimation.Completed -= HideCompleted;
			BeginAnimation(TopProperty, null);
			_toUpAnimation.BeginTime = TimeSpan.Zero;
			Top = -3;
			_toUpAnimation.Completed += HideCompleted;
		}

		/// <summary>
		/// Slides out the window when it loses a focus or is inactive.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void WindowActivityInfoDeactivatedEventHandler( object sender, EventArgs e )
		{
			ResetToUpAnimation();
			BeginAnimation(TopProperty, _toUpAnimation);
		}

		/// <summary>
		/// Hides the window when when it is slided out of the screen.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void HideCompleted(object sender, EventArgs e)
		{
			Hide();
		}

		/// <summary>
		/// Custom show method in order to show Acitivty Info Box, reset animation and hide it after 4 seconds being inactive.
		/// </summary>
		public void ShowActivityInfoBox()
		{
			_toUpAnimation.Completed -= HideCompleted;
			BeginAnimation( TopProperty, null );
			Top = -3;
			Show();
			_toUpAnimation.Completed += HideCompleted;

			_toUpAnimation.BeginTime = _hideTime;
			BeginAnimation( TopProperty, _toUpAnimation );
		}

		void ActivityInfoBoxOnActivated( object sender, EventArgs e )
		{
			ResetToUpAnimation();
		}
	}
}