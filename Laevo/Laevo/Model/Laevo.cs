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

		readonly List<IAttentionShift<object>> _attentionShifts;
		public readonly ReadOnlyCollection<IAttentionShift<object>> AttentionShifts;


		public Laevo()
		{
			_activities = new List<Activity>();
			Activities = new ReadOnlyCollection<Activity>( _activities );

			_attentionShifts = new List<IAttentionShift<object>>();
			AttentionShifts = new ReadOnlyCollection<IAttentionShift<object>>( _attentionShifts );

			// Add activities from previous sessions.);
			if ( File.Exists( ActivitiesFile ) )
			{
				using ( var activityFileStream = new FileStream( ActivitiesFile, FileMode.Open ) )
				{
					var existingActivities = (List<Activity>)ActivitySerializer.ReadObject( activityFileStream );
					existingActivities.ForEach( AddActivity );
				}
			}

			// Attention was shifted to Laevo since the user launched it.
			ShiftAttention( this );
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
			AddActivity( activity );

			CurrentActivity = activity;
			return activity;
		}

		void AddActivity( Activity activity )
		{
			// TODO: Unhook event once activities can be deleted.
			activity.OpenedEvent += ShiftAttention;

			_activities.Add( activity );
		}

		/// <summary>
		///   Indicates an attention shift of the user towards a new object.
		/// </summary>
		/// <typeparam name = "T">The type of the object the user's attention shifted to.</typeparam>
		/// <param name = "object">The object the user's attention shifted to.</param>
		void ShiftAttention<T>( T @object )
			where T : class
		{
			_attentionShifts.Add( new AttentionShift<T>( DateTime.Now, @object ) );
		}

		public void Persist()
		{
			using ( var activityFileStream = new FileStream( ActivitiesFile, FileMode.Create ) )
			{
				ActivitySerializer.WriteObject( activityFileStream, _activities );
			}
		}

		public void Exit()
		{
			// Attention shift to 'null' is used to indicate the application was shut down.
			ShiftAttention<object>( null );

			Persist();
		}
	}
}
