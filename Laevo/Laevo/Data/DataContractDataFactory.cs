﻿using ABC.Interruptions;
using ABC.Windows.Desktop;
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
		readonly InterruptionAggregator _interruptionAggregator;


		public DataContractDataFactory( string dataFolder, InterruptionAggregator interruptionAggregator )
		{
			_dataFolder = dataFolder;
			_interruptionAggregator = interruptionAggregator;
		}


		public IModelRepository CreateModelRepository()
		{
			return new DataContractSerializedModelRepository( _dataFolder, _interruptionAggregator );
		}

		public IViewRepository CreateViewRepository( IModelRepository linkedModelRepository, VirtualDesktopManager desktopManager )
		{
			return new DataContractSerializedViewRepository( _dataFolder, desktopManager, linkedModelRepository );
		}
	}
}