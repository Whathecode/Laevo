using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using ABC.Interruptions;
using Breakpoints.Aggregator;
using Breakpoints.Breakpoint;
using Breakpoints.Common;
using Breakpoints.Managers;
using DummyImportanceEvaluation;


namespace NotificationManager
{
	public class NotificationManager
	{
		/// <summary>
		/// Event which is triggered when a new notification occurs.
		/// </summary>
		public EventHandler<NotificationEventArgs> NotificationTriggered;

		/// <summary>
		/// Event which is triggered when a breakpoint that contains notifications occurs.
		/// </summary>
		public EventHandler<NotificationEventArgs> NotificationBreakpointTriggered;

		/// <summary>
		/// Registered breakpoint managers connected to the aggregator.
		/// </summary>
		readonly Dictionary<Guid, AbstarctBreakpointManager> _breakpointManagers = new Dictionary<Guid, AbstarctBreakpointManager>();

		/// <summary>
		/// Automatic breakpoint launchers for registered interruptions.
		/// </summary>
		readonly Dictionary<AbstractInterruption, Timer> _notificationTimers = new Dictionary<AbstractInterruption, Timer>();

		/// <summary>
		/// Maximum time limitations for breakpoints types.
		/// </summary>
		readonly Dictionary<BreakpointType, TimeSpan> _breakpointsTimeLimits = new Dictionary<BreakpointType, TimeSpan>
		{
			{ BreakpointType.Fine, TimeSpan.FromMinutes( 10 ) },
			{ BreakpointType.Medium, TimeSpan.FromMinutes( 45 ) },
			{ BreakpointType.Coarse, TimeSpan.FromHours( 2 ) }
		};

		static readonly Queue<AbstractInterruption> FineNotifications = new Queue<AbstractInterruption>();
		static readonly Queue<AbstractInterruption> MediumNotifications = new Queue<AbstractInterruption>();
		static readonly Queue<AbstractInterruption> CoarseNotifications = new Queue<AbstractInterruption>();

		/// <summary>
		/// Registered interruptions with assigned breakpoint types.
		/// </summary>
		readonly Dictionary<BreakpointType, List<Queue<AbstractInterruption>>> _notificationBreakpoints = new Dictionary<BreakpointType, List<Queue<AbstractInterruption>>>
		{
			{
				BreakpointType.Fine, new List<Queue<AbstractInterruption>>
				{
					FineNotifications
				}
			},
			{
				BreakpointType.Medium, new List<Queue<AbstractInterruption>>
				{
					FineNotifications,
					MediumNotifications
				}
			},
			{
				BreakpointType.Coarse, new List<Queue<AbstractInterruption>>
				{
					FineNotifications,
					MediumNotifications,
					CoarseNotifications
				}
			}
		};

		readonly InterruptionAggregator _notificationsAggregator;
		readonly BreakpointAggregator _breakpointsAggregator;

		public NotificationManager( string notificationPluginLibrary, string breakpointPluginLibrary )
		{
			_notificationsAggregator = new InterruptionAggregator( notificationPluginLibrary );
			_breakpointsAggregator = new BreakpointAggregator( breakpointPluginLibrary );

			// Set up interruption handlers.
			_notificationsAggregator.InterruptionReceived += ( sender, notification ) =>
			{
				// TODO: Use proper importance evaluation.
				notification.Importance = ImportanceEvaluator.EvaluatieImportance( notification.Content );

				NotificationTriggered( this, new NotificationEventArgs( null, notification ) );
				RegisterNotification( notification, BreakpointType.Coarse );
			};
		}

		public void UpdateAggregators( DateTime dateTime )
		{
			_notificationsAggregator.Update( dateTime );
		}

		/// <summary>
		/// Adds a new breakpoint manager to the manager.
		/// </summary>
		/// <param name="breakpointManager">Breakpoint manager to add.</param>
		/// <returns>False if a specific type of manager is already added, true otherwise.</returns>
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

		/// <summary>
		/// Removes breakpoint manager from the manager.
		/// </summary>
		/// <param name="breakpointManager">Breakpoint manager to delete.</param>
		/// <returns>True if removed, false otherwise.</returns>
		public bool RemoveManager( AbstarctBreakpointManager breakpointManager )
		{
			if ( _breakpointManagers.Remove( breakpointManager.Guid ) )
				return false;

			breakpointManager.FineBreakpointOccured -= BreakpointOccured;
			breakpointManager.MediumBreakpointOccured -= BreakpointOccured;
			breakpointManager.CoarseBreakpointOccured -= BreakpointOccured;
			breakpointManager.UnregisterAllBreakpoints();
			return true;
		}

		/// <summary>
		/// Gets a specific instance of breakpoint manager.
		/// </summary>
		/// <param name="guid">GUID of a specific breakpoint manager.</param>
		/// <param name="breakpointManager">Output breakpoint manager.</param>
		/// <returns></returns>
		public bool GetBreakpointManager( Guid guid, out AbstarctBreakpointManager breakpointManager )
		{
			return _breakpointManagers.TryGetValue( guid, out breakpointManager );
		}

		void BreakpointOccured( object sender, NotificationEventArgs eventArgs )
		{
			List<Queue<AbstractInterruption>> breakpointQueues;
			if ( !_notificationBreakpoints.TryGetValue( eventArgs.Breakpoint.Type, out breakpointQueues ) )
			{
				return;
			}

			breakpointQueues.ForEach( notifications =>
			{
				var notificationEventArgs = new NotificationEventArgs( eventArgs.Breakpoint );
				while ( notifications.Count != 0 )
				{
					var notification = notifications.Dequeue();
					notificationEventArgs.Notification.Add( notification );

					// Stop and remove automatic interruption launcher.
					var notificationTimer = _notificationTimers.FirstOrDefault( notificationToStop => notificationToStop.Key == notification ).Value;
					if ( notificationTimer != null )
					{
						notificationTimer.Stop();
						_notificationTimers.Remove( notification );
					}
				}
				// Trigger notification breakpoint event with all the notifications.
				NotificationBreakpointTriggered( this, notificationEventArgs );
			} );
		}


		void RegisterNotification( AbstractInterruption notification, BreakpointType breakpointType )
		{
			List<Queue<AbstractInterruption>> breakpointQueue;
			if ( _notificationBreakpoints.TryGetValue( breakpointType, out breakpointQueue ) )
				breakpointQueue.First().Enqueue( notification );

			TimeSpan timeLimit;
			_breakpointsTimeLimits.TryGetValue( breakpointType, out timeLimit );

			// Trigger notification after certain time if breakpoint has not occurred.
			var breakpointLauncherTimer = new Timer
			{
				// TODO: Choose appropriate time span, for now coarse.
				Interval = timeLimit.TotalMilliseconds,
				AutoReset = false
			};
			breakpointLauncherTimer.Elapsed += ( sender, args ) =>
			{
				NotificationBreakpointTriggered( this,
					new NotificationEventArgs( new Breakpoint( DateTime.Now, BreakpointType.None ), notification ) );
				breakpointLauncherTimer.Stop();
			};
			_notificationTimers.Add( notification, breakpointLauncherTimer );
			breakpointLauncherTimer.Start();
		}

		public void ClearBreakpointNotifications()
		{
			FineNotifications.Clear();
			MediumNotifications.Clear();
			CoarseNotifications.Clear();
			_notificationTimers.Values.ToList().ForEach( timer => timer.Stop() );
			_notificationTimers.Clear();
		}
	}
}