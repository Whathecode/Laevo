using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Breakpoints.Common;
using LinFu.Delegates;


namespace Breakpoints.Managers
{
	public abstract class AbstarctBreakpointManager
	{
		/// <summary>
		/// Manager Type.
		/// </summary>
		public abstract Guid Guid { get; }

		/// <summary>
		/// Event which is triggered when a fine breakpoint occurred.
		/// </summary>
		public event EventHandler<BreakpointInterruptionEventArgs> FineBreakpointOccured;

		/// <summary>
		/// Event which is triggered when a medium breakpoint occurred.
		/// </summary>
		public event EventHandler<BreakpointInterruptionEventArgs> MediumBreakpointOccured;

		/// <summary>
		/// Event which is triggered when a Coarse breakpoint occurred.
		/// </summary>
		public event EventHandler<BreakpointInterruptionEventArgs> CoarseBreakpointOccured;

		public ReadOnlyCollection<string> SubscribedEventsNames
		{
			get { return _subscribedEvents.Keys.ToList().AsReadOnly(); }
		}

		readonly Dictionary<string, MulticastDelegate> _subscribedEvents = new Dictionary<string, MulticastDelegate>();

		public ReadOnlyCollection<Breakpoint> OccuredBreakpointsReadOnly
		{
			get { return OccuredBreakpoints.AsReadOnly(); }
		}

		protected readonly List<Breakpoint> OccuredBreakpoints = new List<Breakpoint>();

		protected object OnCoarseBreakpoint( object[] args )
		{
			var breakpoint = new Breakpoint( DateTime.Now, BreakpointType.Coarse );
			OccuredBreakpoints.Add( breakpoint );
			CoarseBreakpointOccured( this, new BreakpointInterruptionEventArgs( breakpoint ) );
			return null;
		}

		protected object OnMediumBreakpoint( object[] args )
		{
			var breakpoint = new Breakpoint( DateTime.Now, BreakpointType.Medium );
			OccuredBreakpoints.Add( breakpoint );
			MediumBreakpointOccured( this, new BreakpointInterruptionEventArgs( breakpoint ) );
			return null;
		}

		protected object OnFineBreakpoint( object[] args )
		{
			var breakpoint = new Breakpoint( DateTime.Now, BreakpointType.Fine );
			OccuredBreakpoints.Add( breakpoint );
			FineBreakpointOccured( this, new BreakpointInterruptionEventArgs( breakpoint ) );
			return null;
		}

		CustomDelegate GetDelegate( BreakpointType breakpointType )
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
				case BreakpointType.Fine:
					deletegteToAssign = OnFineBreakpoint;
					break;
				default:
					deletegteToAssign = OnFineBreakpoint;
					break;
			}
			return deletegteToAssign;
		}

		/// <summary>
		/// Registers a new breakpoint.
		/// </summary>
		/// <param name="eventName">Key identifier for event.</param>
		/// <param name="source">Source of an event.</param>
		/// <param name="breakpointType">Desired breakpoint type.</param>
		/// <returns>True if event have not been registered, false otherwise.</returns>
		public bool RegisterBreakpoint( string eventName, object source, BreakpointType breakpointType )
		{
			if ( _subscribedEvents.ContainsKey( eventName ) )
				return false;

			var deletegteToAssign = GetDelegate( breakpointType );
			_subscribedEvents.Add( eventName , EventBinder.BindToEvent( eventName, source, deletegteToAssign ));
			return true;
		}

		/// <summary>
		/// Unregisters a new breakpoint.
		/// </summary>
		/// <param name="eventName">Key identifier for event.</param>
		/// <param name="source">Source of an event.</param>
		/// <param name="breakpointType">Desired breakpoint type.</param>
		/// <returns>True if event have not been registered, false otherwise.</returns>
		public bool UnregisterBreakpoint( string eventName, object source, BreakpointType breakpointType )
		{
			if ( !_subscribedEvents.ContainsKey( eventName ) )
				return false;

			MulticastDelegate multicastDelegate;
			_subscribedEvents.TryGetValue( eventName, out multicastDelegate );
			EventBinder.UnbindFromEvent( eventName, source, multicastDelegate );
			_subscribedEvents.Remove( eventName );
			return true;
		}
	}
}