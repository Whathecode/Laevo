using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Breakpoints.Common;
using LinFu.Delegates;
using Whathecode.System.Extensions;


namespace Breakpoints.Managers
{
	public abstract class AbstarctBreakpointManager : IDisposable
	{
		public abstract bool PredictBreakpoint();

		/// <summary>
		/// Manager Type.
		/// </summary>
		public abstract ManagerType BreakpintType { get; }

		/// <summary>
		/// Event which is triggered when a breakpoint occurred.
		/// </summary>
		public event EventHandler<BreakpointEventArgs> BreakpointOccured;

		public ReadOnlyCollection<string> SubscribedEvents
		{
			get { return _subscribedEvents.Keys.ToList().AsReadOnly(); }
		}
		readonly Dictionary<string, DelegateSource> _subscribedEvents = new Dictionary<string, DelegateSource>();

		public ReadOnlyCollection<Breakpoint> OccuredBreakpointsReadOnly
		{
			get { return OccuredBreakpoints.AsReadOnly(); }
		}
		protected readonly List<Breakpoint> OccuredBreakpoints = new List<Breakpoint>();

		protected object OnCoarseBreakpoint( object[] args )
		{
			var breakpoint = new Breakpoint( DateTime.Now, BreakpointType.Coarse );
			OccuredBreakpoints.Add( breakpoint );
			BreakpointOccured( this, new BreakpointEventArgs( breakpoint ) );
			return null;
		}

		protected object OnMediumBreakpoint( object[] args )
		{
			var breakpoint = new Breakpoint( DateTime.Now, BreakpointType.Medium );
			OccuredBreakpoints.Add( breakpoint );
			BreakpointOccured( this, new BreakpointEventArgs( breakpoint ) );
			return null;
		}

		protected object OnFineBreakpoint( object[] args )
		{
			var breakpoint = new Breakpoint( DateTime.Now, BreakpointType.Fine );
			OccuredBreakpoints.Add( breakpoint );
			BreakpointOccured( this, new BreakpointEventArgs( breakpoint ) );
			return null;
		}

		public void RegisterBreakpoint( string eventName, object source, BreakpointType breakpointType )
		{
			CustomDelegate deletegteToAssign;
			switch ( breakpointType )
			{
				case BreakpointType.Coarse:
					deletegteToAssign = OnCoarseBreakpoint;
					break;
				case BreakpointType.Medium:
					deletegteToAssign = OnMediumBreakpoint;
					break;
				default:
					deletegteToAssign = OnFineBreakpoint;
					break;
			}
			_subscribedEvents.Add( eventName, new DelegateSource( source, deletegteToAssign ) );
			EventBinder.BindToEvent( eventName, source, deletegteToAssign );
		}


		protected void RaiseBreakpointEvent( AbstarctBreakpointManager breakpointManager, BreakpointEventArgs eventArgs )
		{
			BreakpointOccured( breakpointManager, eventArgs );
		}

		public void Dispose()
		{
			_subscribedEvents.ForEach( subscribedEvent => EventBinder.UnbindFromEvent( subscribedEvent.Key, subscribedEvent.Value.Source, subscribedEvent.Value.Delegate ) );
			_subscribedEvents.Clear();
		}
	}
}