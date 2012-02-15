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
		// TODO: Use user document path.
		const string ActivityContextPath = @"C:\Users\Steven\Documents\Whathecode\Laevo\Activities";


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
			: this( String.Format( "{0:s}", DateTime.Now ) ) { }

		public Activity( string name )
		{
			Name = name;
			DataPaths = new List<Uri>();
			DateCreated = DateTime.Now;

			// Create initial data path.
			string safeName = PathHelper.ReplaceInvalidChars( name, '-' );
			string path = Path.Combine( ActivityContextPath, safeName ).MakeUnique( p => !Directory.Exists( p ), "_i" );
			DirectoryInfo activityDirectory = new DirectoryInfo( path );
			activityDirectory.Create();
			DataPaths.Add( new Uri( activityDirectory.FullName ) );
		}
	}
}
