using System;


namespace Breakpoints.Managers
{
	public class LaevoBreakpointManager : AbstarctBreakpointManager
	{
		readonly Guid _guid;

		public LaevoBreakpointManager( Guid guid )
		{
			_guid = guid;
		}

		public override Guid Guid
		{
			get { return _guid; }
		}
	}
}