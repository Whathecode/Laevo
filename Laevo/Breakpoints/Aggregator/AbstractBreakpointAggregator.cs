using System;
using System.Collections.Generic;
using System.Linq;
using ABC.Plugins;
using Breakpoints.Managers;


namespace Breakpoints.Aggregator
{
	public abstract class AbstractBreakpointAggregator
	{
		public List<Type> GetBreakpointManagerTypes()
		{
			return GetBreakpointManagers()
				.SelectMany( h => PluginHelper<AbstarctBreakpointManager>.SafePluginInvoke( h, t => t.GetBreakpointManagerTypes() ) )
				.ToList();
		}

		protected abstract List<AbstarctBreakpointManager> GetBreakpointManagers();
	}
}
