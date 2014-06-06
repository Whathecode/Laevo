using System;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using NLog;


namespace Laevo.Data.Common
{
	class PersistanceHelper
	{
		static readonly Logger Log = LogManager.GetCurrentClassLogger();
		
		public const string BackupExtention = ".backup";

		public static void Persist( string file, DataContractSerializer serializer, object toSerialize )
		{
			// Make a backup first, so if serialization fails, no data is lost.
			string backupFile = LaevoController.BackupFolder + file.Substring( file.LastIndexOf( '\\' ) ) + BackupExtention;
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
			catch ( Exception exception )
			{
				if ( File.Exists( backupFile ) )
				{
					File.Delete( file );
					File.Move( backupFile, file );
				}
				Log.ErrorException( "Serialization failed.", exception );
				throw new PersistenceException( "Serialization of data to file \"" + file + "\" failed. Recent data will be lost.", exception );
			}
		}

		public static object ReadData( string filePath, DataContractSerializer serializer, Type returnType )
		{
			try
			{
				return Deserialize( filePath, serializer );
			}
			catch ( Exception exception)
			{
				Log.ErrorException( "Deserialization failed.", exception );
				string backupPath = LaevoController.BackupFolder + filePath.Substring( filePath.LastIndexOf( '\\' ) ) + PersistanceHelper.BackupExtention;
				if ( File.Exists( backupPath ) )
				{
					try
					{
						return Deserialize( backupPath, serializer );
					}
					catch ( Exception backupException )
					{
						Log.ErrorException( "Deserialization from backup failed.", backupException );
						return EmptyDeserialization( returnType, filePath );
					}
				}
			}
			return EmptyDeserialization( returnType, filePath );
		}

		static object EmptyDeserialization( Type returnType, string filePath )
		{
			MessageBox.Show( "Deserialization of the file " + filePath + " failed. Program will start without previous history.", "Laevo", MessageBoxButton.OK );
			return Activator.CreateInstance( returnType );
		}

		static object Deserialize( string filePath, DataContractSerializer serializer )
		{
			using ( var fileStream = new FileStream( filePath, FileMode.Open ) )
			{
				return serializer.ReadObject( fileStream );
			}
		}
	}
}