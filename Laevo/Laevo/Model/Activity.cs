using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ABC.Interruptions;
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
		public static readonly string ProgramMyDocumentsFolder = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), "Laevo" );
		static readonly string ActivityContextPath = Path.Combine( ProgramMyDocumentsFolder, "Activities" );

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
		List<Interval<DateTime>> _openIntervals;
		/// <summary>
		///   The intervals during which the activity was open, but not necessarily active.
		/// </summary>
		public IReadOnlyCollection<Interval<DateTime>> OpenIntervals
		{
			get { return _openIntervals; }
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

		/// <summary>
		///   All paths to relevant data sources which are part of this activity context.
		/// </summary>
		[DataMember]
		List<Uri> _dataPaths;


		public Activity()
			: this( "" ) { }

		public Activity( string name )
		{
			SetDefaults();

			Name = name;
			DateCreated = DateTime.Now;

			// Create initial data path.
			var activityDirectory = new DirectoryInfo( CreateSafeFolderName() );
			activityDirectory.Create();
			SpecificFolder = new Uri( activityDirectory.FullName );
			_dataPaths.Add( SpecificFolder );
		}


		[OnDeserializing]
		void OnDeserializing( StreamingContext context )
		{
			SetDefaults();
		}

		void SetDefaults()
		{
			_dataPaths = new List<Uri>();
			_openIntervals = new List<Interval<DateTime>>();
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
			_currentOpenInterval = new Interval<DateTime>( now, now );
			_openIntervals.Add( _currentOpenInterval );
			IsOpen = true;

			OpenedEvent( this );
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

		public void Plan( DateTime atTime, TimeSpan duration )
		{
			// Set the planned time as an interval when the activity will be open.
			_openIntervals.Clear();
			_currentOpenInterval = new Interval<DateTime>( atTime, atTime + duration );
			_openIntervals.Add( _currentOpenInterval );
		}

		/// <summary>
		///   Merges the passed activity into this activity.
		/// </summary>
		/// <param name = "activity">The activity to merge into this activity.</param>
		public void Merge( Activity activity )
		{
			_interruptions.AddRange( activity.Interruptions );
			_dataPaths.AddRange( activity._dataPaths );
		}

		/// <summary>
		///   Return all paths to relevant data sources which are part of this activity context.
		/// </summary>
		public ReadOnlyCollection<Uri> GetUpdatedDataPaths()
		{
			var existingPaths = _dataPaths.Where( p => Directory.Exists( p.LocalPath ) ).ToList();
			_dataPaths = existingPaths;

			if ( SpecificFolder != null && !Directory.Exists( SpecificFolder.LocalPath ) )
			{
				SpecificFolder = null;
			}
			else
			{
				AttemptActivityFolderRename();
			}

			return _dataPaths.AsReadOnly();
		}

		/// <summary>
		///   Update the data sources which are part of this activity context. The existing paths will be replaced by the new ones.
		///   Additionally, the activity-specific folder will be attempted to be renamed to the latest activity name.
		/// </summary>
		/// <param name = "paths">The paths to replace the existing context paths with.</param>
		public void SetNewDataPaths( List<Uri> paths )
		{
			_dataPaths = new List<Uri>();
			_dataPaths.AddRange( paths );

			// Check whether the activity-specific folder was removed.
			if ( !paths.Contains( SpecificFolder ) )
			{
				SpecificFolder = null;
			}
			else
			{
				AttemptActivityFolderRename();
			}
		}

		/// <summary>
		///   Attempt to rename the activity-specific folder representing the activity name.
		/// </summary>
		public void AttemptActivityFolderRename()
		{
			// No activity-specific folder set?
			if ( SpecificFolder == null )
			{
				return;
			}

			// No new name set?
			string desiredName = CreateFolderName();
			string currentName = SpecificFolder.LocalPath.Split( Path.DirectorySeparatorChar ).Last();
			if ( desiredName == currentName )
			{
				return;
			}

			// Attempt rename.
			string newFolder = CreateSafeFolderName();
			if ( newFolder == currentName )
			{
				// The current folder name is already a 'safe' desired name.
				return;
			}
			var currentPath = new DirectoryInfo( SpecificFolder.LocalPath );
			try
			{
				currentPath.MoveTo( newFolder );
				_dataPaths.Remove( SpecificFolder );
				SpecificFolder = new Uri( newFolder );
				_dataPaths.Add( SpecificFolder );
			}
			catch ( IOException )
			{
				// Try again next time. The directory might be in use.
			}
		}

		public void Update( DateTime now )
		{
			if ( IsOpen )
			{
				_currentOpenInterval.ExpandTo( now );
			}
		}

		public void AddInterruption( AbstractInterruption interruption )
		{
			_interruptions.Add( interruption );
		}
	}
}
