using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using ABC.Plugins;
using Breakpoints.Managers;


namespace Breakpoints.Aggregator
{
	public class BreakpointAggregator : AbstractBreakpointAggregator
	{
		readonly DirectoryCatalog _pluginCatalog;

		[ImportMany( AllowRecomposition = true )]
		readonly List<AbstarctBreakpointManager> _breakpointManagers =
			new List<AbstarctBreakpointManager>();


		public string PluginFolderPath
		{
			get { return _pluginCatalog.FullPath; }
		}

		public BreakpointAggregator( string pluginFolderPath )
		{
			_pluginCatalog = CompositionHelper.CreateDirectory( pluginFolderPath );
			CompositionHelper.ComposeFromPath( this, _pluginCatalog );
		}

		public void Refresh()
		{
			_pluginCatalog.Refresh();
		}

		AbstarctBreakpointManager GetBreakpointManager( Guid guid )
		{
			return _breakpointManagers
				.FirstOrDefault( breakpointManager => breakpointManager.AssemblyInfo.Guid == guid );
		}

		public Version GetPluginVersion( Guid guid )
		{
			var plugin = GetBreakpointManager( guid );
			return plugin != null ? plugin.AssemblyInfo.Version : null;
		}

		public IInstallable GetInstallablePlugin( Guid guid )
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			return GetBreakpointManager( guid ) as IInstallable;
		}

		public string GetPluginPath( Guid guid )
		{
			return _pluginCatalog.LoadedFiles.FirstOrDefault( loadedFile => loadedFile.IndexOf( guid.ToString(), StringComparison.OrdinalIgnoreCase ) >= 0 );
		}

		protected override List<AbstarctBreakpointManager> GetBreakpointManagers()
		{
			return _breakpointManagers;
		}
	}
}