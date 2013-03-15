using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;
using Whathecode.System.IO;


namespace Laevo.Model
{
	/// <summary>
	///   Class containing all the data relating to one activity context.
	/// </summary>
	/// <author>Steven Jeuris</author>
	[DataContract]
	class Activity
	{
		readonly string _activityContextPath 
			= Path.Combine( Laevo.ProgramDataFolder, "Activities" );

		public event Action<Activity> OpenedEvent;
		public event Action<Activity> ClosedEvent;

		public event Action<Activity> ActivatedEvent;
		public event Action<Activity> DeactivatedEvent;

		/// <summary>
		///   A name describing this activity.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		///   Determines whether or not the activity is currently open, but not necessarily active (working on it).
		/// </summary>
		public bool IsOpen { get; private set; }

		/// <summary>
		///   Determines whether or not the activity is currently active (working on it).
		/// </summary>
		public bool IsActive { get; private set; }

		/// <summary>
		///   The date when this activity was first created.
		/// </summary>
		[DataMember]
		public DateTime DateCreated { get; private set; }

		Interval<DateTime> _currentOpenInterval;
		[DataMember]
		readonly List<Interval<DateTime>> _openIntervals = new List<Interval<DateTime>>();

		/// <summary>
		///   The intervals during which the activity was open, but not necessarily active.
		/// </summary>
		public ReadOnlyCollection<Interval<DateTime>> OpenIntervals
		{
			get { return _openIntervals.AsReadOnly(); }
		}

		/// <summary>
		///   All paths to relevant data sources which are part of this activity context.
		/// </summary>
		[DataMember]
		public List<Uri> DataPaths { get; private set; }


		public Activity()
			: this( "" ) { }

		public Activity( string name )
		{
			Name = name;
			DataPaths = new List<Uri>();
			DateCreated = DateTime.Now;

			// Create initial data path.
			string folderName = name;
			if ( folderName.Length == 0 )
			{
				folderName = DateTime.Now.ToString( "g" );
			}
			string safeName = PathHelper.ReplaceInvalidChars( folderName, '-' );
			string path = Path.Combine( _activityContextPath, safeName ).MakeUnique( p => !Directory.Exists( p ), "_i" );
			var activityDirectory = new DirectoryInfo( path );
			activityDirectory.Create();
			DataPaths.Add( new Uri( activityDirectory.FullName ) );
		}

		/// <summary>
		///   Make an activity active (look at it) without opening it. It won't be made part of the current multitasking session.
		/// </summary>
		public void View()
		{
			if ( !IsActive )
			{
				IsActive = true;
				ActivatedEvent( this );
			}
		}

		/// <summary>
		///   Activating an activity opens it, thus making it part of the current multitasking session, and also makes it active (look at it).
		/// </summary>
		public void Activate()
		{
			Open();
			View();
		}

		/// <summary>
		///   Deactivating an activity indicates it is no longer being looked at.
		/// </summary>
		public void Deactivate()
		{
			if ( IsActive )
			{
				IsActive = false;
				DeactivatedEvent( this );
			}
		}

		/// <summary>
		///   Opening an activity makes it part of the current multitasking session.
		/// </summary>
		public void Open()
		{
			if ( IsOpen )
			{
				return;
			}

			var now = DateTime.Now;
			_currentOpenInterval = new Interval<DateTime>( now, now );
			_openIntervals.Add( _currentOpenInterval );
			IsOpen = true;
			//if ( OpenedEvent != null )	// HACK: Why does OpenedEvent become null? The aspect should prevent that!
			//{
				OpenedEvent( this );
			//}
		}

		/// <summary>
		///   Closing an activity removes it from the currently ongoing multitasking session.
		/// </summary>
		public void Close()
		{
			if ( !IsOpen )
			{
				return;
			}

			_currentOpenInterval.ExpandTo( DateTime.Now );
			_currentOpenInterval = null;
			IsOpen = false;
			ClosedEvent( this );
		}

		internal void Update( DateTime now )
		{
			if ( IsOpen )
			{
				_currentOpenInterval.ExpandTo( now );
			}
		}
	}
}
