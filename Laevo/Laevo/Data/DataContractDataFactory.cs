using ABC.Applications.Persistence;
using ABC.Interruptions;
using ABC.Workspaces;
using Laevo.Data.Model;
using Laevo.Data.View;


namespace Laevo.Data
{
	/// <summary>
	///   Creates data repositories persisted to flat files through DataContracts.
	/// </summary>
	class DataContractDataFactory : IDataFactory
	{
		readonly string _dataFolder;
		readonly AbstractInterruptionAggregator _interruptionAggregator;
		readonly AbstractPersistenceProvider _persistenceProvider;


		public DataContractDataFactory( string dataFolder, AbstractInterruptionAggregator interruptionAggregator, AbstractPersistenceProvider persistenceProvider )
		{
			_dataFolder = dataFolder;
			_interruptionAggregator = interruptionAggregator;
			_persistenceProvider = persistenceProvider;
		}


		public IModelRepository CreateModelRepository()
		{
			return new DataContractSerializedModelRepository( _dataFolder, _interruptionAggregator );
		}

		public IViewRepository CreateViewRepository( IModelRepository linkedModelRepository, WorkspaceManager workspaceManager )
		{
			return new DataContractSerializedViewRepository( _dataFolder, workspaceManager, linkedModelRepository, _persistenceProvider );
		}
	}
}
