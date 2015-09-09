using System;

namespace Breakpoints.Common
{
	[Flags]
	public enum ManagerType
	{
		Laevo = 1,
		Win32 = 2,
		VisualStudio = 4,
		Chrome = 6
	}
}
