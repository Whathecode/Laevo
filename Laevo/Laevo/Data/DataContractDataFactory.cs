﻿using ABC.Applications.Persistence;
using ABC.Interruptions;
using ABC.Workspaces;
using Laevo.Data.Model;
using Laevo.Data.View;
using Laevo.Peer;


namespace Laevo.Data
{
	/// <summary>
	///   Creates data repositories persisted to flat files through DataContracts.
	/// </summary>
	public class DataContractDataFactory : IDataFactory
	{
		readonly string _dataFolder;
		readonly InterruptionAggregator _interruptionAggregator;
		readonly PersistenceProvider _persistenceProvider;


	    public DataContractDataFactory( string dataFolder, InterruptionAggregator interruptionAggregator, PersistenceProvider persistenceProvider )
		{
			_dataFolder = dataFolder;
			_interruptionAggregator = interruptionAggregator;
			_persistenceProvider = persistenceProvider;
		}


		public IModelRepository CreateModelRepository()
		{
            return new DataContractSerializedModelRepository(_dataFolder, _interruptionAggregator);
		}

		public IViewRepository CreateViewRepository( IModelRepository linkedModelRepository, WorkspaceManager workspaceManager )
		{
			return new DataContractSerializedViewRepository( _dataFolder, workspaceManager, linkedModelRepository, _persistenceProvider );
		}
	}
}
