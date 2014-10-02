using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using Laevo.View.Main;
using Laevo.ViewModel.Main;
using Whathecode.System.Aspects;


[assembly: InitializeEventHandlers( AttributeTargetTypes = "Laevo.*" )]

namespace Laevo
{
	/// <summary>
	///   Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		Model.Laevo _model;
		MainViewModel _viewModel;
		TrayIconControl _trayIcon;


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

			// TODO: Remove this temporary hack. It is used only for ClickOnce publish feature. 
			// RequestedExecutionLevel in the manifest file should be set to highestPossible instead of this.
			var windowsIdentity = WindowsIdentity.GetCurrent();
			if ( windowsIdentity != null )
			{
				var windowsPrincipal = new WindowsPrincipal( windowsIdentity );

				bool runAsAdmin = windowsPrincipal.IsInRole( WindowsBuiltInRole.Administrator );

				if ( !runAsAdmin )
				{
					// It is not possible to launch a ClickOnce app as administrator directly,
					// so instead we launch the app as administrator in a new process.
					var processInfo = new ProcessStartInfo( Assembly.GetExecutingAssembly().CodeBase )
					{
						UseShellExecute = true,
						Verb = "runas"
					};

					// The following properties run the new process as administrator

					// Start the new process
					try
					{
						Process.Start( processInfo );
					}
					catch ( Exception )
					{
						// The user did not allow the application to run as administrator
						MessageBox.Show( "Please run Laevo with administrator privileges." );
					}

					// Shut down the current process
					Current.Shutdown();
					return;
				}

				// We are running as administrator
				// TODO: Support multiple languages, for now force english.
				var english = new CultureInfo( "en-US" );
				Thread.CurrentThread.CurrentCulture = english;

				// Create exception logger.
				DispatcherUnhandledException += ( s, a )
					=> File.AppendAllText( Path.Combine( Model.Laevo.ProgramDataFolder, "log.txt" ), a.Exception.ToString() + Environment.NewLine );

				// Create Model.
				_model = new Model.Laevo();

				// Create ViewModel.
				_viewModel = new MainViewModel( _model );

				// Create View.
				_trayIcon = new TrayIconControl( _viewModel ) { DataContext = _viewModel };
			}
			else
			{
				// The user did not allow the application to run as administrator
				MessageBox.Show( "Laevo is not able to determine your user privileges, please run application as an administrator!" );
				Current.Shutdown();
			}
		}

		protected override void OnExit( ExitEventArgs e )
		{
			_viewModel.Dispose();
			_trayIcon.Dispose();
		}
	}
}