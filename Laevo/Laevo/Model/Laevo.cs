using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;


namespace Laevo.Model
{
	/// <summary>
	///   The main model of the Laevo Virtual Desktop Manager.
	/// </summary>
	/// <author>Steven Jeuris</author>
	class Laevo
	{
		public static readonly string ProgramDataFolder
			= Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Laevo" );
		static readonly string ActivitiesFile = Path.Combine( ProgramDataFolder, "Activities.xml" );

		static readonly DataContractSerializer ActivitySerializer = new DataContractSerializer( typeof( List<Activity> ) );
		readonly List<Activity> _activities;
		public readonly ReadOnlyCollection<Activity> Activities;

		public Activity CurrentActivity { get; private set; }


		public Laevo()
		{
			_activities = new List<Activity>();
			Activities = new ReadOnlyCollection<Activity>( _activities );

			// Add activities from previous sessions.
			if ( File.Exists( ActivitiesFile ) )
			{
				using ( var activityFileStream = new FileStream( ActivitiesFile, FileMode.Open ) )
				{
					var existingActivities = (List<Activity>)ActivitySerializer.ReadObject( activityFileStream );
					_activities.AddRange( existingActivities );
				}
			}

			// Create startup activity.
			var startup = new Activity( "Startup" );
			CurrentActivity = startup;
			_activities.Add( startup );
		}


		public void Update()
		{
			_activities.ForEach( a => a.Update() );
		}

		/// <summary>
		///   Creates a new activity and sets it as the current activity.
		/// </summary>
		/// <returns>The newly created activity.</returns>
		public Activity CreateNewActivity()
		{
			var activity = new Activity();
			_activities.Add( activity );

			CurrentActivity = activity;
			return activity;
		}

		public void Persist()
		{
			using ( var activityFileStream = new FileStream( ActivitiesFile, FileMode.Create ) )
			{
				ActivitySerializer.WriteObject( activityFileStream, _activities );
			}
		}
	}
}
