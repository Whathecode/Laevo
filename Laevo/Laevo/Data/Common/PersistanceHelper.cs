using System;
using System.IO;
using System.Runtime.Serialization;


namespace Laevo.Data.Common
{
	class PersistanceHelper
	{
		public static void Persist( string file, DataContractSerializer serializer, object toSerialize )
		{
			// Make a backup first, so if serialization fails, no data is lost.
			string backupFile = file + ".backup";
			if ( File.Exists( file ) )
			{
				File.Copy( file, backupFile, true );
			}

			// Serialize the data.
			try
			{
				using ( var fileStream = new FileStream( file, FileMode.Create ) )
				{
					serializer.WriteObject( fileStream, toSerialize );
				}
			}
			catch ( Exception e )
			{
				if ( File.Exists( backupFile ) )
				{
					File.Delete( file );
					File.Move( backupFile, file );
				}
				throw new PersistenceException( "Serialization of data to file \"" + file + "\" failed. Recent data will be lost.", e );
			}

			// Remove temporary backup file.
			if ( File.Exists( backupFile ) )
			{
				File.Delete( backupFile );
			}
		}
	}
}