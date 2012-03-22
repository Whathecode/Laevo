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
		protected override void OnStartup( StartupEventArgs e )
		{
			base.OnStartup(e);
			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			// Create Model.
			var model = new Model.Laevo();

			// Create ViewModel.
			var mainViewModel = new MainViewModel( model );

			// Create View.
			new TrayIconControl { DataContext = mainViewModel };
		}
	}
}