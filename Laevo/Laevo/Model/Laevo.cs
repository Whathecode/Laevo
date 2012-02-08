using System.Collections.ObjectModel;


namespace Laevo.Model
{
	/// <summary>
	///   The main model of the Laevo Virtual Desktop Manager.
	/// </summary>
	/// <author>Steven Jeuris</author>
	class Laevo
	{
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

			// TODO: Remove temporary activities.
			CurrentActivity = new Activity();
			_activities.Add( CurrentActivity );
			_activities.Add( new Activity() );
			_activities.Add( new Activity() );
			_activities.Add( new Activity() );
		}
	}
}
