using System;
using System.Collections.Generic;
using ABC.Interruptions;


namespace Breakpoints.Common
{
	public class NotificationEventArgs : EventArgs
	{
		public Breakpoint.Breakpoint Breakpoint { get; private set; }
		public List<AbstractInterruption> Notification { get; private set; }

		public NotificationEventArgs( Breakpoint.Breakpoint breakpoint, AbstractInterruption notification )
		{
			Notification = new List<AbstractInterruption> { notification };
			Breakpoint = breakpoint;
		}

		public NotificationEventArgs( Breakpoint.Breakpoint breakpoint )
		{
			Notification = new List<AbstractInterruption>();
			Breakpoint = breakpoint;
		}
	}
}