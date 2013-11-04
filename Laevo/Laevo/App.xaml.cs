using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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

			// TODO: Support multiple languages, for now force english.
			var english = new CultureInfo( "en-US" );
			Thread.CurrentThread.CurrentCulture = english;

			// Create exception logger.
			DispatcherUnhandledException += ( s, a )
                => File.AppendAllText(Path.Combine(Model.Laevo.ProgramLocalDataFolder, "log.txt"), a.Exception.ToString() + Environment.NewLine);

			// Create Model.
			_model = new Model.Laevo();

			// Create ViewModel.
			_viewModel = new MainViewModel( _model );

			// Create View.
			_trayIcon = new TrayIconControl( _viewModel ) { DataContext = _viewModel };
		}

		protected override void OnExit( ExitEventArgs e )
		{
			_viewModel.Dispose();
			_trayIcon.Dispose();
		}
	}
}