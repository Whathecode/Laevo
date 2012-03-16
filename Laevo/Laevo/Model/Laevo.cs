using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;


namespace Laevo.Model
{
	/// <summary>
	///   The main model of the Laevo Virtual Desktop Manager.
	/// </summary>
	/// <author>Steven Jeuris</author>
	class Laevo
	{
		public static readonly string ProgramData
			= Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Laevo" );

		readonly List<Activity> _activities;
		public readonly ReadOnlyCollection<Activity> Activities;

		public Activity CurrentActivity { get; private set; }


		public Laevo()
		{
			_activities = new List<Activity>();
			Activities = new ReadOnlyCollection<Activity>( _activities );

			// Create startup activity.
			var startup = new Activity( "Home" );
			CurrentActivity = startup;
			_activities.Add( startup );
		}


		public void Update()
		{
			_activities.ForEach( a => a.Update() );
		}
	}
}
