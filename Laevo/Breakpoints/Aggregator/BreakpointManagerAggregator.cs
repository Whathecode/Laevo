
using System;
using System.Collections.Generic;
using Breakpoints.Common;
using Breakpoints.Managers;


namespace Breakpoints.Aggregator
{
	class BreakpointManagerAggregator
	{

		readonly Dictionary<ManagerType, AbstarctBreakpointManager> _breakpointManagers = new Dictionary<ManagerType, AbstarctBreakpointManager>(); 
		public void AddManager( AbstarctBreakpointManager breakpointManager )
		{
			_breakpointManagers.Add( breakpointManager.BreakpintType, breakpointManager );
		}

		public void RemoveManager( ManagerType managerType )
		{
			_breakpointManagers.Remove( managerType );
		}

		public bool PredictBreakpoint( TimeSpan timeSpan, BreakpointType breakpointType, ManagerType managerType = ManagerType.Laevo )
		{
			return true;
		}
	}
}
