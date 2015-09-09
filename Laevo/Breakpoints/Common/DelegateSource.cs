using System;

namespace Breakpoints.Common
{
	public class DelegateSource
	{
		public object Source { get; private set; }
		public MulticastDelegate Delegate { get; private set; }

		public DelegateSource( object source, MulticastDelegate @delegate )
		{
			Source = source;
			Delegate = @delegate;
		}
	}
}
