using System;
using ABC.Interruptions;


namespace Breakpoints.Common
{
	public class BreakpointInterruptionEventArgs : EventArgs
	{
		public Breakpoint Breakpoint { get; private set; }
		public AbstractInterruption Interruption { get; private set; }

		public BreakpointInterruptionEventArgs( Breakpoint breakpoint, AbstractInterruption interruption )
		{
			Breakpoint = breakpoint;
			Interruption = interruption;
		}

		public BreakpointInterruptionEventArgs( Breakpoint breakpoint )
		{
			Breakpoint = breakpoint;
		}
	}
}
