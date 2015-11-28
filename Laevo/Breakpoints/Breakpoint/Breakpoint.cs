using System;


namespace Breakpoints.Breakpoint
{
	public class Breakpoint
	{
		public DateTime Occurence { private set; get; }
		public BreakpointType Type { private set; get; }

		public Breakpoint( DateTime occurence, BreakpointType type )
		{
			Occurence = occurence;
			Type = type;
		}
	}
}
