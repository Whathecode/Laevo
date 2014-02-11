﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Whathecode.System.Aspects;


[assembly: InitializeEventHandlers( AttributeTargetTypes = "Laevo.*" )]

namespace Laevo
{
	/// <summary>
	///   Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
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


		protected override void OnStartup( StartupEventArgs e )
		{
			base.OnStartup( e );
			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			Current.Resources[ "AeroThemeColor" ] = new SolidColorBrush( GetWindowColorizationColor( false ) );

			// Verify whether application is already running.
			// TODO: Improved verification, rather than just name.
			if ( Process.GetProcessesByName( "Laevo" ).Count() > 1 )
			{
				MessageBox.Show( "Laevo is already running.", "Laevo", MessageBoxButton.OK );

				Current.Shutdown();
				return;
			}

			// TODO: Support multiple languages, for now force english.
			var english = new CultureInfo( "en-US" );
			Thread.CurrentThread.CurrentCulture = english;

			// Create exception logger.
			DispatcherUnhandledException += ( s, a )
				=> File.AppendAllText( Path.Combine( Model.Laevo.ProgramLocalDataFolder, "log.txt" ), a.Exception.ToString() + Environment.NewLine );

			// Initiate the controller which sets up the MVVM classes.
			_controller = new LaevoController();

			// Hack- Create empty window to catch aero theme color changes.
			var hiddenWindow = new Window { ShowActivated = false, Focusable = false, Visibility = Visibility.Hidden, Width = 0, Height = 0, Left = -100, Top = -100 };
			hiddenWindow.Show();

			// Register empty window hooks.
			var mainWindowPointer = new WindowInteropHelper( hiddenWindow ).Handle;
			var mainWindowSource = HwndSource.FromHwnd( mainWindowPointer );
			if ( mainWindowSource != null )
			{
				mainWindowSource.AddHook( HandleChangedColor );
			}
		}

		/// <summary>
		/// Catches changes of aero theme colors.
		/// </summary>
		/// <param name="hwnd"></param>
		/// <param name="msg"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <param name="handled"></param>
		/// <returns></returns>
		IntPtr HandleChangedColor( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled )
		{
			switch ( msg )
			{
					//TODO: Investigate if case below ever happens. Documentation is wrong?
				case AeroColorChanged:
					Current.Resources[ "AeroThemeColor" ] = new SolidColorBrush( GetWindowColorizationColor( false ) );
					return IntPtr.Zero;
					// Case when aero theme color is actually changed.
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