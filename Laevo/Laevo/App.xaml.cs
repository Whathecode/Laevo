using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Whathecode.System.Aspects;


[assembly: InitializeEventHandlers( AttributeTargetTypes = "Laevo.*" )]
namespace Laevo
{
	/// <summary>
	///   Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		LaevoController _controller;

		protected override void OnStartup( StartupEventArgs e )
		{
			base.OnStartup( e );
			ShutdownMode = ShutdownMode.OnExplicitShutdown;

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
		}

		protected override void OnExit( ExitEventArgs e )
		{
			_controller.Dispose();
		}
	}
}