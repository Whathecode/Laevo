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

		public event Action<Activity> ActivatedEvent;

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


		public void Open()
		{
			if ( !IsOpen )
			{
				var now = DateTime.Now;
				_currentOpenInterval = new Interval<DateTime>( now, now );
				_openIntervals.Add( _currentOpenInterval );
				IsOpen = true;
				if ( OpenedEvent != null )	// HACK: Why does OpenedEvent become null? The aspect should prevent that!
				{
					OpenedEvent( this );
				}
			}

			// Opening an activity also activates it.
			if ( !IsActive )
			{
				IsActive = true;
				ActivatedEvent( this );
			}
		}

		public void Deactivate()
		{
			IsActive = false;
		}

		public void Close()
		{
			if ( !IsOpen )
			{
				return;
			}

			_currentOpenInterval.ExpandTo( DateTime.Now );
			_currentOpenInterval = null;
			IsOpen = false;
			IsActive = false;
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
