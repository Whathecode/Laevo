using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using ABC.Interruptions;
using Breakpoints.Common;
using Breakpoints.Managers;


namespace Breakpoints.Aggregator
{
	public class BreakpointManagerAggregator
	{
		/// <summary>
		/// Event which is triggered when a breakpoint which is assigned to the interruption occurs.
		/// </summary>
		public EventHandler<BreakpointInterruptionEventArgs> InterruptionBreakpointOccured;

		/// <summary>
		/// Registered breakpoint managers connected to the aggregator.
		/// </summary>
		readonly Dictionary<Guid, AbstarctBreakpointManager> _breakpointManagers = new Dictionary<Guid, AbstarctBreakpointManager>();

		/// <summary>
		/// Automatic breakpoint launchers for registered interruptions.
		/// </summary>
		readonly Dictionary<AbstractInterruption, Timer> _interruptionTimers = new Dictionary<AbstractInterruption, Timer>();

		/// <summary>
		/// Maximum time limitations for breakpoints types.
		/// </summary>
		readonly Dictionary<BreakpointType, TimeSpan> _breakpointsTimeLimits = new Dictionary<BreakpointType, TimeSpan>
		{
			{ BreakpointType.Fine, TimeSpan.FromMinutes( 1 ) },
			{ BreakpointType.Medium, TimeSpan.FromMinutes( 2.5 ) },
			{ BreakpointType.Coarse, TimeSpan.FromMinutes( 5 ) }
		};

		static readonly Queue<AbstractInterruption> FineInterruptions = new Queue<AbstractInterruption>();
		static readonly Queue<AbstractInterruption> MediumInterruptions = new Queue<AbstractInterruption>();
		static readonly Queue<AbstractInterruption> CoarseInterruptions = new Queue<AbstractInterruption>();

		/// <summary>
		/// Registered interruptions with assigned breakpoint types.
		/// </summary>
		readonly Dictionary<BreakpointType, List<Queue<AbstractInterruption>>> _interruptionBreakpoints = new Dictionary<BreakpointType, List<Queue<AbstractInterruption>>>
		{
			{
				BreakpointType.Fine, new List<Queue<AbstractInterruption>>
				{
					FineInterruptions
				}
			},
			{
				BreakpointType.Medium, new List<Queue<AbstractInterruption>>
				{
					FineInterruptions,
					MediumInterruptions
				}
			},
			{
				BreakpointType.Coarse, new List<Queue<AbstractInterruption>>
				{
					FineInterruptions,
					MediumInterruptions,
					CoarseInterruptions
				}
			}
		};


		/// <summary>
		/// Adds a new breakpoint manager to the aggregator.
		/// </summary>
		/// <param name="breakpointManager">Breakpoint manager instance.</param>
		/// <returns>True if a specific type of manager is not already added, false otherwise.</returns>
		public bool AddManager( AbstarctBreakpointManager breakpointManager )
		{
			if ( _breakpointManagers.ContainsKey( breakpointManager.Guid ) )
				return false;

			_breakpointManagers.Add( breakpointManager.Guid, breakpointManager );
			breakpointManager.FineBreakpointOccured += BreakpointOccured;
			breakpointManager.MediumBreakpointOccured += BreakpointOccured;
			breakpointManager.CoarseBreakpointOccured += BreakpointOccured;

			return true;
		}

		public bool RemoveManager( AbstarctBreakpointManager breakpointManager )
		{
			if ( _breakpointManagers.Remove( breakpointManager.Guid ) )
				return false;

			breakpointManager.FineBreakpointOccured -= BreakpointOccured;
			breakpointManager.MediumBreakpointOccured -= BreakpointOccured;
			breakpointManager.CoarseBreakpointOccured -= BreakpointOccured;
			return true;
		}

		public bool GetBreakpointManager( Guid guid, out AbstarctBreakpointManager breakpointManager )
		{
			return _breakpointManagers.TryGetValue( guid, out breakpointManager );
		}

		void BreakpointOccured( object sender, BreakpointInterruptionEventArgs eventArgs )
		{
			List<Queue<AbstractInterruption>> breakpointQueues;
			if ( !_interruptionBreakpoints.TryGetValue( eventArgs.Breakpoint.Type, out breakpointQueues ) )
			{
				return;
			}

			breakpointQueues.ForEach( breakpointQueue =>
			{
				while ( breakpointQueue.Count != 0 )
				{
					var interruption = breakpointQueue.Dequeue();

					// Trigger interruption event.
					var newEventArgs = new BreakpointInterruptionEventArgs( eventArgs.Breakpoint, interruption );
					InterruptionBreakpointOccured( this, newEventArgs );

					// Stop and remove automatic interruption launcher.
					var interruptinTimer = _interruptionTimers.FirstOrDefault( interruptionTimer => interruptionTimer.Key == interruption ).Value;
					if ( interruptinTimer != null )
					{
						interruptinTimer.Stop();
						_interruptionTimers.Remove( interruption );
					}
				}
			} );
		}


		public bool RegisterInterruption( AbstractInterruption interruption, BreakpointType breakpointType )
		{
			List<Queue<AbstractInterruption>> breakpointQueue;
			if ( _interruptionBreakpoints.TryGetValue( breakpointType, out breakpointQueue ) )
				breakpointQueue.First().Enqueue( interruption );

			//_interruptionBreakpoints.Add( interruption, breakpointType );
			TimeSpan timeLimit;
			_breakpointsTimeLimits.TryGetValue( breakpointType, out timeLimit );

			// Trigger breakpoint after certain time if it has not occurred.
			var breakpointLauncherTimer = new Timer
			{
				// TODO: Choose appropriate time span, for now coarse.
				Interval = timeLimit.TotalMilliseconds,
				AutoReset = false
			};
			breakpointLauncherTimer.Elapsed += ( sender, args ) =>
			{
				InterruptionBreakpointOccured( this,
					new BreakpointInterruptionEventArgs( new Breakpoint( DateTime.Now, BreakpointType.None ), interruption ) );
				breakpointLauncherTimer.Stop();
			};
			_interruptionTimers.Add( interruption, breakpointLauncherTimer );
			breakpointLauncherTimer.Start();

			return true;
		}
	}
}