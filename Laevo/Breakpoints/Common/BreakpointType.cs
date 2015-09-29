using System;

namespace Breakpoints.Common
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
