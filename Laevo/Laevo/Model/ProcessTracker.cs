﻿using System;
using System.Management;


namespace Laevo.Model
{
	public class ProcessTracker
	{
		ManagementEventWatcher _startWatcher;
		ManagementEventWatcher _stopWatcher;

		public event Action<ProcessInfo> ProcessStarted;
		public event Action<ProcessInfo> ProcessStopped;


		public void Start()
		{
			var interval = new TimeSpan( 0, 0, 1 );
			const string isWin32Process = "TargetInstance isa \"Win32_Process\"";

			// Listen for started processes.
			WqlEventQuery startQuery = new WqlEventQuery( "__InstanceCreationEvent", interval, isWin32Process );
			_startWatcher = new ManagementEventWatcher( startQuery );
			_startWatcher.Start();
			_startWatcher.EventArrived += OnStartEventArrived;

			// Listen for closed processes.
			WqlEventQuery stopQuery = new WqlEventQuery( "__InstanceDeletionEvent", interval, isWin32Process );
			_stopWatcher = new ManagementEventWatcher( stopQuery );
			_stopWatcher.Start();
			_stopWatcher.EventArrived += OnStopEventArrived;

		}

		public void Stop()
		{
			_startWatcher.Stop();
			_stopWatcher.Stop();
		}

		void OnStartEventArrived( object sender, EventArrivedEventArgs e )
		{
			var o = (ManagementBaseObject)e.NewEvent[ "TargetInstance" ];

			ProcessStarted( RetrieveProcessInfo( o ) );
		}

		void OnStopEventArrived( object sender, EventArrivedEventArgs e )
		{
			var o = (ManagementBaseObject)e.NewEvent[ "TargetInstance" ];

			ProcessStopped( RetrieveProcessInfo( o ) );
		}

		static ProcessInfo RetrieveProcessInfo( ManagementBaseObject o )
		{
			return new ProcessInfo(
				Convert.ToInt32( o[ "ProcessId" ] ),
				(string)o[ "Name" ] );
		}
	}
}
