using System;
using System.IO;
using ABC.Applications;
using ABC.Interruptions;
using Laevo.Data;
using Laevo.Data.Model;
using Laevo.Data.View;
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
		static readonly string InterruptionsPluginLibrary = Path.Combine( ProgramLocalDataFolder, "InterruptionHandlers" );
		static readonly string PersistencePluginLibrary = Path.Combine( ProgramLocalDataFolder, "ApplicationPersistence" );

		readonly MainViewModel _viewModel;
		readonly TrayIconControl _trayIcon;


		public LaevoController()
		{
			// Create Services.
			var interruptionAggregator = new InterruptionAggregator( InterruptionsPluginLibrary );
			var applicationPersistence = new PersistenceProvider( PersistencePluginLibrary );
			//var repositoryFactory = new DataContractDataFactory( ProgramLocalDataFolder, interruptionAggregator );
			var repositoryFactory = new ScrumExampleDataFactory();
			
			// Create Model.
			IModelRepository dataRepository = repositoryFactory.CreateModelRepository();
			var model = new Model.Laevo( ProgramLocalDataFolder, dataRepository, interruptionAggregator, applicationPersistence );

			// Create ViewModel.
			// TODO: Move DesktopManager to ViewModel?
			IViewRepository viewDataRepository = repositoryFactory.CreateViewRepository( dataRepository, model.DesktopManager );
			_viewModel = new MainViewModel( model, viewDataRepository );

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
