using System;
using Breakpoints.Common;


namespace Breakpoints.Managers
{
	public class LaevoBreakpointManager : AbstarctBreakpointManager
	{
		readonly ManagerType _type;

		public LaevoBreakpointManager()
		{
			_type = ManagerType.Laevo;
		}

		public override bool PredictBreakpoint()
		{
			throw new NotImplementedException();
		}

		public override ManagerType BreakpintType
		{
			get { return _type; }
		}
	}
}