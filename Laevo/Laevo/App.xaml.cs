﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Laevo.Logging;
using NLog;


namespace Laevo
{
	/// <summary>
	///   Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		static readonly Logger Log = LogManager.GetCurrentClassLogger();

		const int AeroColorChanged = 0x320;
		const int AeroColorChanged2 = 26;

		LaevoController _controller;

		[DllImport( "dwmapi.dll", EntryPoint = "#127" )]
		static extern void GetAeroThemeColors( out AeroColors parameters );

		public struct AeroColors
		{
			public uint
				Color,
				Afterglow,
				ColorBalance,
				AfterglowBalance,
				BlurBalance,
				GlassReflectionIntensity,
				OpaqueBlend;
		}


		static App()
		{
			NLog.Config.ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition( "fullcontext", typeof( FullContextLayoutRenderer ) );
		}


		protected override void OnStartup( StartupEventArgs e )
		{
			base.OnStartup( e );
			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			Current.Resources[ "AeroThemeColor" ] = new SolidColorBrush( GetWindowColorizationColor( false ) );

			// Verify whether application is already running.
			// TODO: Improved verification, rather than just name.
			if ( Process.GetProcessesByName( "Laevo" ).Count() > 1 )
			{
				View.MessageBox.Show( "Laevo is already running.", "Laevo", MessageBoxButton.OK );

				Current.Shutdown();
				return;
			}

			// TODO: Support multiple languages, for now force english.
			var english = new CultureInfo( "en-US" );
			Thread.CurrentThread.CurrentCulture = english;

			// Initiate the controller which sets up the MVVM classes.
			_controller = new LaevoController();

			AppDomain.CurrentDomain.UnhandledException += ( s, a ) =>
			{
				Log.Fatal( "Unhandled exception.", a.ExceptionObject as Exception );

				// Try to bring back all open windows on unhandled exceptions.
				_controller.ExitDesktopManager();

				OnExit( null );
			};

			// HACK: Create empty window to catch aero theme color changes.
			var hiddenWindow = new Window { ShowActivated = false, Focusable = false, Visibility = Visibility.Hidden, Width = 0, Height = 0, Left = -100, Top = -100 };
			hiddenWindow.Show();
			var mainWindowPointer = new WindowInteropHelper( hiddenWindow ).Handle;
			var mainWindowSource = HwndSource.FromHwnd( mainWindowPointer );
			if ( mainWindowSource != null )
			{
				mainWindowSource.AddHook( HandleChangedColor );
			}

			// Hook to an event rised when user shuts down a computer or logs out in order to exit application properly. 
			SessionEnding += ( o, args ) =>
			{
				Log.InfoWithData( "Windows session ended.", new LogData( "Reason", args.ReasonSessionEnding ) );
				_controller.Exit();
			};
		}

		/// <summary>
		/// Catches changes of aero theme colors.
		/// </summary>
		IntPtr HandleChangedColor( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
		{
			switch ( msg )
			{
				case AeroColorChanged:
					// TODO: Investigate whether this case ever happens. Documentation is wrong?
				case AeroColorChanged2:
					Current.Resources[ "AeroThemeColor" ] = new SolidColorBrush( GetWindowColorizationColor( false ) );
					return IntPtr.Zero;

				default:
					return IntPtr.Zero;
			}
		}

		/// <summary>
		/// Gets a color from windows registry in order to apply it to a window in both Aero and othere themes.
		/// </summary>
		Color GetWindowColorizationColor( bool opaque )
		{
			AeroColors aeroColors;
			GetAeroThemeColors( out aeroColors );

			return Color.FromArgb(
				(byte)( opaque ? 255 : aeroColors.Color >> 24 ),
				(byte)( aeroColors.Color >> 16 ),
				(byte)( aeroColors.Color >> 8 ),
				(byte)aeroColors.Color );
		}

		protected override void OnExit( ExitEventArgs e )
		{
			_controller.Dispose();
		}
	}
}