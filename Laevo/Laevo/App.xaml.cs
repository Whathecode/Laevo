using System.Windows;
using Laevo.View.Main;
using Laevo.ViewModel.Main;


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

			// TODO: Create Model.

			// Create ViewModel.
			var mainViewModel = new MainViewModel();

			// Create View.
			new TrayIconControl { DataContext = mainViewModel };
		}
	}
}