using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ABC.Applications.Persistence;
using SHDocVw;
using Whathecode.Interop;
using Whathecode.System.Extensions;


namespace ABC.Applications.Explorer
{
	public struct ExplorerLocation
	{
		public string LocationName { get; set; }
		public string LocationUrl { get; set; }
		public byte[] Pidl { get; set; }
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
				LocationUrl = window.LocationURL,
				Pidl = window.GetPidl()
			};

			window.Quit();

			return persistedData;
		}

		public override void Resume( string applicationPath, object persistedData )
		{
			var location = (ExplorerLocation)persistedData;

			// Copy the PIDL to unmanaged memory.
			IntPtr pidl = Marshal.AllocHGlobal( location.Pidl.Length );
			Marshal.Copy( location.Pidl, 0, pidl, location.Pidl.Length );
			
			// Open an explorer window with the passed PIDL.
			var executeInfo = new Shell32.ShellExecuteInfo
			{
				Type = Shell32.ShellExecuteInfoType.Pidl,
				Pidl = pidl,
				Show = User32.WindowState.ShowNoActivate,
				StructSize = (uint)Marshal.SizeOf( typeof( Shell32.ShellExecuteInfo ) )
			};
			Shell32.ShellExecuteEx( ref executeInfo );
			
			Marshal.Release( pidl );
		}

		public override Type GetPersistedDataType()
		{
			return typeof( ExplorerLocation );
		}
	}
}
