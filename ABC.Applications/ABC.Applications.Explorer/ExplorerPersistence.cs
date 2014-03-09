using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using ABC.Applications.Persistence;
using Microsoft.Win32;
using SHDocVw;
using Whathecode.System.Diagnostics;
using Whathecode.System.Extensions;


namespace ABC.Applications.Explorer
{
	public struct ExplorerLocation
	{
		public string LocationName { get; set; }
		public string LocationUrl { get; set; }
	}


	[Export( typeof( AbstractApplicationPersistence ) )]
	public class ExplorerPersistence : AbstractApplicationPersistence
	{
		// TODO: The _shellFolderNames collection is currently filled up in the constructor, but is only a hackish attempt at finding out which windows is open when LocationURL is not specified.
		//       Can we somehow find out the actual thing visible in the explorer window? http://stackoverflow.com/q/22284718/590790
		readonly Dictionary<string, string> _shellFolderNames = new Dictionary<string, string>(); 


		public ExplorerPersistence()
			: base( "explorer" )
		{
			// First try to find CLSIDs, which don't always appear to work but seem to be more complete.
			using ( RegistryKey clsids = Registry.ClassesRoot.OpenSubKey( "CLSID" ) )
			{
				foreach ( string clsid in clsids.GetSubKeyNames().Where( clsid => clsids.OpenSubKey( clsid + "\\ShellFolder" ) != null ) )
				{
					using ( RegistryKey shellFolder = clsids.OpenSubKey( clsid ) )
					{
						// Try to use the localized name, otherwise the default name.
						string name = shellFolder.LoadMuiStringValue( "LocalizedString" ) ?? (string)shellFolder.GetValue( "" );
						if ( name != null )
						{
							_shellFolderNames[ name ] = "::" + clsid;
						}
					}
				}
			}

			// Alternatively, load shell description names which can be opened using the "shell:Name" parameter.
			using ( RegistryKey clsids = Registry.LocalMachine.OpenSubKey( @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FolderDescriptions" ) )
			{
				foreach ( string clsid in clsids.GetSubKeyNames() )
				{
					using ( RegistryKey folderDescription = clsids.OpenSubKey( clsid ) )
					{
						string localized = folderDescription.LoadMuiStringValue( "LocalizedName" );
						if ( localized != null )
						{
							_shellFolderNames[ localized ] = "shell:" + (string)folderDescription.GetValue( "Name" );
						}
					}
				}
			}
		}


		public override object Suspend( SuspendInformation toSuspend )
		{
			// TODO: Is there a safer way to guarantee that it is actually the internet explorer we expect it to be?
			// Exactly one matching window should be found, otherwise something went wrong.
			var shellWindows = new ShellWindows();
			var window = shellWindows
				.Cast<InternetExplorer>()
				.First( e =>
					Path.GetFileNameWithoutExtension( e.FullName ).IfNotNull( p => p.ToLower() ) == "explorer" // For some reason, the process CAN be both "Explorer", and "explorer".
					&& toSuspend.Windows.First().Handle.Equals( new IntPtr( e.HWND ) ) );

			var persistedData = new ExplorerLocation
			{
				LocationName = window.LocationName,
				LocationUrl = window.LocationURL
			};

			window.Quit();

			return persistedData;
		}

		public override void Resume( string applicationPath, object persistedData )
		{
			var location = (ExplorerLocation)persistedData;

			// Start out assuming explorer points to a simple path.
			string openFolder = location.LocationUrl;

			// Check whether the open folder is a shell folder.
			if ( String.IsNullOrEmpty( openFolder ) && _shellFolderNames.ContainsKey( location.LocationName ) )
			{
				openFolder = _shellFolderNames[ location.LocationName ];
			}

			ProcessHelper.SetUp( applicationPath, openFolder ).Run();
		}

		public override Type GetPersistedDataType()
		{
			return typeof( ExplorerLocation );
		}
	}
}
