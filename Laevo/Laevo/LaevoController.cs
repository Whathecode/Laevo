using System;
using System.IO;
using ABC.Interruptions;
using Laevo.Data;
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
		static readonly string ProgramLocalDataFolder
			= Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Laevo" );
		static readonly string PluginLibrary = Path.Combine( ProgramLocalDataFolder, "InterruptionHandlers" );

		readonly MainViewModel _viewModel;
		readonly TrayIconControl _trayIcon;


		public LaevoController()
		{
			// Create Services.
			var interruptionAggregator = new InterruptionAggregator( PluginLibrary );
			IDataRepository dataRepository = new DataContractSerializedRepository( ProgramLocalDataFolder, interruptionAggregator );

			// Create Model.
			var model = new Model.Laevo( ProgramLocalDataFolder, dataRepository, interruptionAggregator );

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
