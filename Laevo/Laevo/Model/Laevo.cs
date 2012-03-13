using System;
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
		public static readonly string ProgramData = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Laevo" );

		readonly ObservableCollection<Activity> _activities;
		readonly ReadOnlyObservableCollection<Activity> _readOnlyActivities;
		public ReadOnlyObservableCollection<Activity> Activities
		{
			get { return _readOnlyActivities; }
		}

		public Activity CurrentActivity { get; private set; }


		public Laevo()
		{
			_activities = new ObservableCollection<Activity>();
			_readOnlyActivities = new ReadOnlyObservableCollection<Activity>( _activities );

			// TODO: Remove demo activities.
			var thesis = new Activity( "Thesis" );
			var programming = new Activity( "Programming" );
			var browsing = new Activity( "browsing" );
			_activities.Add( thesis );
			_activities.Add( programming );
			_activities.Add( browsing );
			CurrentActivity = programming;
		}
	}
}
