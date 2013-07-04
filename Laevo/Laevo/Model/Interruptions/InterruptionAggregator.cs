using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Threading;


namespace Laevo.Model.Interruptions
{
	/// <summary>
	///   Aggregates interruptions raised by externally loaded plugins.
	/// </summary>
	class InterruptionAggregator : AbstractInterruptionHandler
	{
		static readonly string PluginLibrary = Path.Combine( Laevo.ProgramDataFolder, "InterruptionHandlers" );

		readonly CompositionContainer _pluginContainer;

		[ImportMany]
		readonly List<AbstractInterruptionHandler> _interruptionHandlers = new List<AbstractInterruptionHandler>();


		public InterruptionAggregator()
		{
			// Set up plugin container.
			if ( !Directory.Exists( PluginLibrary ) )
			{
				Directory.CreateDirectory( PluginLibrary );
			}
			var catalog = new DirectoryCatalog( PluginLibrary );
			_pluginContainer = new CompositionContainer( catalog );
			_pluginContainer.ComposeParts( this );

			// Initialize loaded interruption handlers.
			foreach ( var handler in _interruptionHandlers )
			{
				handler.InterruptionReceived += TriggerInterruption;
			}
		}


		public override void Update( DateTime now )
		{
			if ( !Monitor.TryEnter( this ) )
			{
				return;
			}

			foreach ( var handler in _interruptionHandlers )
			{
				handler.Update( now );
			}

			Monitor.Exit( this );
		}
	}
}
