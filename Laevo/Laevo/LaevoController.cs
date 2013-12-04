using Laevo.View.Main;
using Laevo.ViewModel.Main;
using Whathecode.System;


namespace Laevo
{
	/// <summary>
	///   The main access point of Laevo, which acts as a controller hooking the different components of the Model/View/ViewModel together.
	/// </summary>
	class LaevoController : AbstractDisposable
	{
		readonly MainViewModel _viewModel;
		readonly TrayIconControl _trayIcon;


		public LaevoController()
		{
			// Create Model.
			var model = new Model.Laevo();

			// Create ViewModel.
			_viewModel = new MainViewModel( model );

			// Create View.
			_trayIcon = new TrayIconControl( _viewModel ) { DataContext = _viewModel };
		}

		protected override void FreeManagedResources()
		{
			// Nothing to do.
		}

		protected override void FreeUnmanagedResources()
		{
			_viewModel.Dispose();
			_trayIcon.Dispose();
		}
	}
}
