using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ABC.Interruptions;
using Laevo.Logging;
using NLog;
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
	public class Activity
	{
		static readonly Logger Log = LogManager.GetCurrentClassLogger();

		public static readonly string ProgramMyDocumentsFolder = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), "Laevo" );
		static readonly string ActivityContextPath = Path.Combine( ProgramMyDocumentsFolder, "Activities" );

		public event Action<Activity> OpenedEvent;
		public event Action<Activity> StoppedEvent;

		public event Action<Activity> ActivatedEvent;
		public event Action<Activity> DeactivatedEvent;

		public event Action<Activity> ToDoChangedEvent;

		[DataMember]
		public Guid Identifier { get; private set; }

		[DataMember]
		string _name;
		/// <summary>
		///   A name describing this activity.
		/// </summary>
		public string Name
		{
			get { return _name; }
			set
			{
				Log.InfoWithData( "Name changed.", new LogData( this ), new LogData( "New name", value, "Anonymized" ) );
				_name = value;
			}
		}

		/// <summary>
		///   Determines whether or not the activity is currently open, but not necessarily active (working on it).
		/// </summary>
		public bool IsOpen { get; private set; }

		/// <summary>
		///   Determines whether or not the activity is currently active (working on it).
		/// </summary>
		public bool IsActive { get; private set; }

		[DataMember]
		bool _isToDo;
		/// <summary>
		///   Determines whether or not the activity is a to-do item, meaning that currently it is not open, nor is work planned on it in the future at a specific interval.
		///   When work will continue on the activity is undecided.
		/// </summary>
		public bool IsToDo
		{
			get { return _isToDo; }
			private set
			{
				if ( value != _isToDo )
				{
					_isToDo = value;
					ToDoChangedEvent( this );
				}
			}
		}

		/// <summary>
		///   The date when this activity was first created.
		/// </summary>
		[DataMember]
		public DateTime DateCreated { get; private set; }

		TimeInterval _currentOpenInterval;

		[DataMember]
		List<TimeInterval> _openIntervals;

		/// <summary>
		///   The intervals during which the activity was open, but not necessarily active.
		/// </summary>
		public IReadOnlyCollection<TimeInterval> OpenIntervals
		{
			get { return _openIntervals; }
		}

		[DataMember]
		List<PlannedInterval> _plannedIntervals;

		/// <summary>
		///   The intervals during which the activity is planned.
		/// </summary>
		public IReadOnlyCollection<PlannedInterval> PlannedIntervals
		{
			get { return _plannedIntervals; }
		}

		[DataMember]
		List<AbstractInterruption> _interruptions;

		/// <summary>
		///   Interruptions which interrupted the activity.
		/// </summary>
		public IReadOnlyCollection<AbstractInterruption> Interruptions
		{
			get { return _interruptions; }
		}

		/// <summary>
		///   The specific folder created for this activity when it was first created. This is the default location where it's context is stored.
		///   It can become null once it is removed by the user. This folder will be removed when the activity is deleted.
		/// </summary>
		[DataMember]
		public Uri SpecificFolder { get; private set; }


		public Activity()
			: this( "" ) {}

		public Activity( string name )
		{
			SetDefaults();

			_name = name; // Change field rather than property to prevent logging activity creation as a name change.
			Identifier = Guid.NewGuid();
			DateCreated = DateTime.Now;

			// Create initial data path.
			var activityDirectory = new DirectoryInfo( CreateSafeFolderName() );
			activityDirectory.Create();
			SpecificFolder = new Uri( activityDirectory.FullName );
		}


		[OnDeserializing]
		void OnDeserializing( StreamingContext context )
		{
			SetDefaults();
		}

		void SetDefaults()
		{
			_openIntervals = new List<TimeInterval>();
			_plannedIntervals = new List<PlannedInterval>();
			_interruptions = new List<AbstractInterruption>();
		}

		string CreateSafeFolderName()
		{
			string currentName = SpecificFolder.IfNotNull( f => f.LocalPath.Split( Path.DirectorySeparatorChar ).Last() );

			return Path.Combine( ActivityContextPath, CreateFolderName() ).MakeUnique( p =>
			{
				string dirName = new Uri( p ).LocalPath.Split( Path.DirectorySeparatorChar ).Last();
				return
					( currentName != null && dirName != null && dirName.Equals( currentName ) ) // The current 'safe' name is already a desired name.
					|| !Directory.Exists( p );
			}, "_i" );
		}

		string CreateFolderName()
		{
			string folderName = DateCreated.ToString( "d" ) + " " + Name;
			string safeName = PathHelper.ReplaceInvalidChars( folderName, '-' );

			// Cut a folder name in order not to exceed max path length.
			const int maxPathLength = 259;
			int maxFolderNameLength = maxPathLength - ActivityContextPath.Length;
			if ( safeName.Length > maxFolderNameLength )
			{
				safeName = safeName.Remove( maxFolderNameLength );
			}

			return safeName;
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
			_currentOpenInterval = new TimeInterval( now, now );
			_openIntervals.Add( _currentOpenInterval );

			IsToDo = false;
			IsOpen = true;
			OpenedEvent( this );

			Log.InfoWithData( "Opened.", new LogData( this ) );
		}

		/// <summary>
		///   Stopping an activity removes it from the currently ongoing multitasking session.
		/// </summary>
		public void Stop()
		{
			if ( !IsOpen )
			{
				return;
			}

			_openIntervals[ _openIntervals.Count - 1 ] = _currentOpenInterval.ExpandTo( DateTime.Now );
			_currentOpenInterval = null;
			IsOpen = false;
			StoppedEvent( this );

			Log.InfoWithData( "Stopped.", new LogData( this ) );
		}

		public void AddPlannedInterval( DateTime atTime, TimeSpan duration )
		{
			if ( atTime < DateTime.Now )
			{
				throw new InvalidOperationException( "A planned interval needs to lie in the future." );
			}

			var plannedInterval = new PlannedInterval( atTime, atTime + duration );
			_plannedIntervals.Add( plannedInterval );

			IsToDo = false;

			Log.InfoWithData( "Added planned interval.", new LogData( this ) );
		}

		/// <summary>
		///   Turns the activity into a to-do item. This removes all future planned intervals, as a to-do items implies it is unknown when work will continue.
		///   In case the activity is open, it is also stopped.
		/// </summary>
		public void MakeToDo()
		{
			Stop();

			DateTime now = DateTime.Now;
			_plannedIntervals.RemoveAll( p => p.Interval.Start > now );
			IsToDo = true;

			Log.InfoWithData( "Made to-do.", new LogData( this ) );
		}

		/// <summary>
		///   Removes all planned intervals (or to do state) of this activity.
		/// </summary>
		public void RemovePlanning()
		{
			if ( IsToDo )
			{
				IsToDo = false;
			}
			else
			{
				DateTime now = DateTime.Now;
				_plannedIntervals.RemoveAll( p => p.Interval.Start > now );

				Log.InfoWithData( "Removed planning.", new LogData( this ) );
			}
		}

		/// <summary>
		///   Merges the passed activity into this activity.
		/// </summary>
		/// <param name = "activity">The activity to merge into this activity.</param>
		public void Merge( Activity activity )
		{
			_interruptions.AddRange( activity.Interruptions );

			Log.InfoWithData( "Activities merged.", new LogData( "Source", activity ), new LogData( "Target", this ) );
		}

		/// <summary>
		///   Attempts to update the specific folder of this activity, renaming it to the latest activity name.
		/// </summary>
		public Uri UpdateSpecificFolder()
		{
			// Verify whether activity specific folder was removed.
			if ( SpecificFolder != null && !Directory.Exists( SpecificFolder.LocalPath ) )
			{
				SpecificFolder = null;
			}
			if ( SpecificFolder == null )
			{
				return SpecificFolder;
			}

			// Verify whether the desired name is already set.
			string desiredName = CreateFolderName();
			string currentName = SpecificFolder.LocalPath.Split( Path.DirectorySeparatorChar ).Last();
			if ( desiredName == currentName )
			{
				return SpecificFolder;
			}

			// Attempt rename.
			string newFolder = CreateSafeFolderName();
			if ( newFolder.Split( Path.DirectorySeparatorChar ).Last() == currentName )
			{
				// The current folder name is already a 'safe' desired name.
				return SpecificFolder;
			}
			var currentPath = new DirectoryInfo( SpecificFolder.LocalPath );
			try
			{
				currentPath.MoveTo( newFolder );
				SpecificFolder = new Uri( newFolder );
			}
			catch ( IOException )
			{
				// Try again next time. The directory might be in use.
			}

			Log.InfoWithData( "New specific folder.", new LogData( this ), new LogData( "Specific folder", SpecificFolder, "" ) );

			return SpecificFolder;
		}

		public void Update( DateTime now )
		{
			if ( IsOpen )
			{
				_currentOpenInterval = _currentOpenInterval.ExpandTo( now );
				_openIntervals[ _openIntervals.Count - 1 ] = _currentOpenInterval;
			}
		}

		public void AddInterruption( AbstractInterruption interruption )
		{
			_interruptions.Add( interruption );
		}

		public override bool Equals( object obj )
		{
			var activity = obj as Activity;

			if ( activity == null )
			{
				return false;
			}

			return Identifier.Equals( activity.Identifier );
		}

		public override int GetHashCode()
		{
			return Identifier.GetHashCode();
		}
	}
}