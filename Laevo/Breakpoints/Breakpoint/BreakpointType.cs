using System;


namespace Breakpoints.Breakpoint
{
	[Flags]
	public enum BreakpointType
	{
		Coarse = 1,
		Medium = 2,
		Fine = 4,
		None = 7
	}
}
