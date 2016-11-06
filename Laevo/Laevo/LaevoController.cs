using System;
using System.IO;
using System.Windows;
using ABC.Applications.Persistence;
using ABC.Interruptions;
using ABC.Workspaces.Windows.Settings;
using Laevo.Data;
using Laevo.Data.Model;
using Laevo.Data.View;
using Laevo.Peer.Mock;
using Laevo.View.Main;
using Laevo.ViewModel.Main;
using Whathecode.System;
using Whathecode.System.Windows;
using Whathecode.System.Extensions;

namespace Laevo
{
	/// <summary>
	///   The main access point of Laevo, which acts as a controller hooking the different components of the Model/View/ViewModel together.
	/// </summary>
	class LaevoController : AbstractDisposable
	{
		public static readonly string ProgramLocalDataFolder
			= Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Laevo" );

		static readonly string InterruptionsPluginLibrary = Path.Combine( ProgramLocalDataFolder, "InterruptionHandlers" );
		static readonly string PersistencePluginLibrary = Path.Combine( ProgramLocalDataFolder, "ApplicationPersistence" );
		static readonly string VdmSettingsLibrary = Path.Combine( ProgramLocalDataFolder, "VdmSettings" );
		public static readonly string PluginManagerPath = Path.Combine( ProgramLocalDataFolder, "PluginManager" );

		readonly AbstractPersistenceProvider _persistenceProvider;

		readonly MainViewModel _viewModel;
		readonly TrayIconControl _trayIcon;
		readonly Model.Laevo _model;

		public LaevoController()
		{
			// Create folders if they do not exist. 
			Directory.CreateDirectory( InterruptionsPluginLibrary );
			Directory.CreateDirectory( PersistencePluginLibrary );
			Directory.CreateDirectory( VdmSettingsLibrary );
			Directory.CreateDirectory( PluginManagerPath );

			// Create extension services.
			var interruptionAggregator = new FolderInterruptionTrigger( InterruptionsPluginLibrary );
			_persistenceProvider = new FolderPersistenceProvider( PersistencePluginLibrary );
			var vdmSettings = new LoadedSettings( false, true, w =>
			{
				string className = new WindowInfo( w.Handle ).GetClassName();
				// TODO: Windows 10 universal apps seem to handle windows differently.
				//       Ignore them for now.
				return !className.EqualsAny( "ApplicationFrameWindow", "Windows.UI.Core.CoreWindow" );
			} );

			var peerFactory = new MockPeerFactory();
			var repositoryFactory = new DataContractDataFactory( ProgramLocalDataFolder, interruptionAggregator, _persistenceProvider, peerFactory );

			// Create Model.
			IModelRepository dataRepository = repositoryFactory.CreateModelRepository();
			_model = new Model.Laevo( dataRepository,
				interruptionAggregator,
				_persistenceProvider,
				vdmSettings,
				peerFactory );

			// Create ViewModel.
			// TODO: Move WorkspaceManager to ViewModel?
			IViewRepository viewDataRepository = repositoryFactory.CreateViewRepository( dataRepository, _model.WorkspaceManager );
			_viewModel = new MainViewModel( _model, viewDataRepository );

			// Create View.
			_trayIcon = new TrayIconControl( _viewModel ) { DataContext = _viewModel };

			// Persist current application state once per 5 minutes. 
			var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
			dispatcherTimer.Tick += ( s, e ) =>
			{
				_viewModel.Persist();
				try
				{
					_model.Persist();
				}
				catch ( PersistenceException pe )
				{
					View.MessageBox.Show( pe.Message, "Saving data failed", MessageBoxButton.OK );
				}
			};
			dispatcherTimer.Interval = new TimeSpan( 0, 5, 0 );
			dispatcherTimer.Start();
		}

		public void Exit()
		{
			_viewModel.Exit();
		}

		public void ExitDesktopManager()
		{
			_model.WorkspaceManager.Close();
		}

		protected override void FreeManagedResources()
		{
			_persistenceProvider.Dispose();
		}

		protected override void FreeUnmanagedResources()
		{
			_viewModel.Dispose();
			_trayIcon.Dispose();
		}
	}
}