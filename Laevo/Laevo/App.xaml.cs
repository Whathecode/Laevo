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

			// TODO: Create Model.

			// Create ViewModel.
			MainViewModel mainViewModel = new MainViewModel();

			// Create View.
			new TrayIconControl { DataContext = mainViewModel };
		}
	}
}