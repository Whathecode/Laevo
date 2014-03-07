using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using ABC.Applications.Persistence;
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
		public ExplorerPersistence()
			: base( "explorer" ) {}


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

			ProcessHelper.SetUp( applicationPath, location.LocationUrl ).Run();
		}

		public override Type GetPersistedDataType()
		{
			return typeof( ExplorerLocation );
		}
	}
}
