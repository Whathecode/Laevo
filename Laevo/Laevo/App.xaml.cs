using System.IO;
using System.Linq;
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


		protected override void OnStartup( StartupEventArgs e )
		{
			base.OnStartup( e );
			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			// Create exception logger.
			DispatcherUnhandledException += ( s, a )
				=> File.AppendAllText( Path.Combine( Model.Laevo.ProgramDataFolder, "log.txt" ), a.Exception.ToString() );

			// Create Model.
			_model = new Model.Laevo();

			// Add or assign startup activity.
			bool createStartupActivity = true;
			if ( _model.Activities.Any() )
			{
				MessageBoxResult result = MessageBox.Show(
					"Do you wish to assign the currently open windows to an existing activity, instead of creating a new activity for them?",
					"Assign startup activity",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question );
				createStartupActivity = result == MessageBoxResult.No;
			}
			if ( createStartupActivity )
			{
				var startup = _model.CreateNewActivity();
				startup.Name = "Startup";
			}

			// Create ViewModel.
			_viewModel = new MainViewModel( _model );
			if ( !createStartupActivity )
			{
				//  When no startup activity is created, the user needs to select an existing activity to assign the current desktop to.
				_viewModel.SelectActivity( a => a.OpenActivity() );
			}

			// Create View.
			new TrayIconControl { DataContext = _viewModel };
		}

		protected override void OnExit( ExitEventArgs e )
		{
			_viewModel.Dispose();
		}
	}
}