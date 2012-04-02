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
		readonly string _activityContextPath = Path.Combine( Laevo.ProgramDataFolder, "Activities" );


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
		[DataMember]
		public ReadOnlyCollection<Interval<DateTime>> OpenIntervals { get; private set; }

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
			OpenIntervals = new ReadOnlyCollection<Interval<DateTime>>( _openIntervals );

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
			if ( IsOpen )
			{
				return;
			}

			var now = DateTime.Now;
			_currentOpenInterval = new Interval<DateTime>( now, now );
			_openIntervals.Add( _currentOpenInterval );
			IsOpen = true;
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
		}

		internal void Update()
		{
			if ( IsOpen )
			{
				_currentOpenInterval.ExpandTo( DateTime.Now );
			}
		}
	}
}
