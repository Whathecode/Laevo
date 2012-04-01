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
			base.OnStartup(e);
			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			// Create Model.
			_model = new Model.Laevo();

			// Create ViewModel.
			_viewModel = new MainViewModel( _model );

			// Create View.
			new TrayIconControl { DataContext = _viewModel };
		}

		protected override void OnExit( ExitEventArgs e )
		{
			base.OnExit( e );

			_viewModel.Persist();
			_model.Persist();
		}
	}
}