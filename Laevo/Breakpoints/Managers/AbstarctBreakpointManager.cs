using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using ABC.Plugins;
using Breakpoints.Breakpoint;
using Breakpoints.Common;
using LinFu.Delegates;
using Whathecode.System.Extensions;


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
		public event EventHandler<NotificationEventArgs> FineBreakpointOccured;

		/// <summary>
		/// Event which is triggered when a medium breakpoint occurred.
		/// </summary>
		public event EventHandler<NotificationEventArgs> MediumBreakpointOccured;

		/// <summary>
		/// Event which is triggered when a Coarse breakpoint occurred.
		/// </summary>
		public event EventHandler<NotificationEventArgs> CoarseBreakpointOccured;

		public ReadOnlyCollection<string> SubscribedEventsNames
		{
			get { return _subscribedEvents.Keys.ToList().AsReadOnly(); }
		}

		public ReadOnlyCollection<Breakpoint.Breakpoint> OccuredBreakpointsReadOnly
		{
			get { return OccuredBreakpoints.AsReadOnly(); }
		}

		public AssemblyInfo AssemblyInfo { get; private set; }

		protected readonly List<Breakpoint.Breakpoint> OccuredBreakpoints = new List<Breakpoint.Breakpoint>();
		readonly Dictionary<string, MulticastDelegate> _subscribedEvents = new Dictionary<string, MulticastDelegate>();
		protected readonly object Source;

		protected AbstarctBreakpointManager( object source, Assembly assembly )
		{
			Source = source;
			AssemblyInfo = new AssemblyInfo( assembly );
			if ( Guid.Empty == AssemblyInfo.Guid || String.IsNullOrEmpty( AssemblyInfo.TargetProcessName ) )
			{
				throw new ArgumentException( "Plug-in GUID and target process name have to be provided in assembly info." );
			}
		}

		protected object OnCoarseBreakpoint( object[] args )
		{
			var breakpoint = new Breakpoint.Breakpoint( DateTime.Now, BreakpointType.Coarse );
			OccuredBreakpoints.Add( breakpoint );
			if ( CoarseBreakpointOccured != null ) CoarseBreakpointOccured( this, new NotificationEventArgs( breakpoint ) );
			return null;
		}

		protected object OnMediumBreakpoint( object[] args )
		{
			var breakpoint = new Breakpoint.Breakpoint( DateTime.Now, BreakpointType.Medium );
			OccuredBreakpoints.Add( breakpoint );
			if ( MediumBreakpointOccured != null ) MediumBreakpointOccured( this, new NotificationEventArgs( breakpoint ) );
			return null;
		}

		protected object OnFineBreakpoint( object[] args )
		{
			var breakpoint = new Breakpoint.Breakpoint( DateTime.Now, BreakpointType.Fine );
			OccuredBreakpoints.Add( breakpoint );
			if ( FineBreakpointOccured != null ) FineBreakpointOccured( this, new NotificationEventArgs( breakpoint ) );
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
		/// <param name="breakpointType">Desired breakpoint type.</param>
		/// <returns>True if event have not been registered, false otherwise.</returns>
		public bool RegisterBreakpoint( string eventName, BreakpointType breakpointType )
		{
			if ( _subscribedEvents.ContainsKey( eventName ) )
				return false;

			var deletegteToAssign = GetDelegate( breakpointType );
			_subscribedEvents.Add( eventName, EventBinder.BindToEvent( eventName, Source, deletegteToAssign ) );
			return true;
		}

		/// <summary>
		/// Unregisters a new breakpoint.
		/// </summary>
		/// <param name="eventName">Key identifier for event.</param>
		/// <returns>True if event have not been registered, false otherwise.</returns>
		public bool UnregisterBreakpoint( string eventName )
		{
			if ( !_subscribedEvents.ContainsKey( eventName ) )
				return false;

			MulticastDelegate multicastDelegate;
			_subscribedEvents.TryGetValue( eventName, out multicastDelegate );
			EventBinder.UnbindFromEvent( eventName, Source, multicastDelegate );
			_subscribedEvents.Remove( eventName );
			return true;
		}

		public void UnregisterAllBreakpoints()
		{
			SubscribedEventsNames.ForEach( eventName => UnregisterBreakpoint( eventName ) );
		}

		/// <summary>
		///   Returns the types of interruptions this class triggers.
		/// </summary>
		public abstract List<Type> GetBreakpointManagerTypes();
	}
}