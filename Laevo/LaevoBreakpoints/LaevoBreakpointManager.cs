using System;
using System.Collections.Generic;
using System.Reflection;
using Breakpoints.Managers;


namespace LaevoBreakpoints
{
	public class LaevoBreakpointManager<T> : AbstarctBreakpointManager
	{
		readonly Guid _guid;

		public LaevoBreakpointManager( Guid guid, T source )
			: base( source, Assembly.GetExecutingAssembly() )
		{
			_guid = guid;
		}

		public override Guid Guid
		{
			get { return _guid; }
		}

		public override List<Type> GetBreakpointManagerTypes()
		{
			return new List<Type> { typeof( LaevoBreakpointManager<T> ) };
		}

		public T GetSource
		{
			get { return (T)Source; }
		}
	}
}