using System;
using System.Collections.Generic;
using System.IO;
using Whathecode.System.Extensions;
using Whathecode.System.IO;


namespace Laevo.Model
{
	/// <summary>
	///   Class containing all the data relating to one activity context.
	/// </summary>
	/// <author>Steven Jeuris</author>
	class Activity
	{
		readonly string _activityContextPath = Path.Combine( Laevo.ProgramData, "Activities" );


		/// <summary>
		///   A name describing this activity.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		///   The date when this activity was first created.
		/// </summary>
		public DateTime DateCreated { get; private set; }

		/// <summary>
		///   All paths to relevant data sources which are part of this activity context.
		/// </summary>
		public List<Uri> DataPaths { get; private set; }


		public Activity()
			: this( DateTime.Now.ToString( "s" ) ) { }

		public Activity( string name )
		{
			Name = name;
			DataPaths = new List<Uri>();
			DateCreated = DateTime.Now;

			// Create initial data path.
			string safeName = PathHelper.ReplaceInvalidChars( name, '-' );
			string path = Path.Combine( _activityContextPath, safeName ).MakeUnique( p => !Directory.Exists( p ), "_i" );
			var activityDirectory = new DirectoryInfo( path );
			activityDirectory.Create();
			DataPaths.Add( new Uri( activityDirectory.FullName ) );
		}
	}
}
