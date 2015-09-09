using System;

namespace Breakpoints.Common
{
	public class BreakpointEventArgs : EventArgs
	{
		public Breakpoint Breakpoint { get; private set; }

		public BreakpointEventArgs( Breakpoint breakpoint )
		{
			Breakpoint = breakpoint;
		}
	}
}
