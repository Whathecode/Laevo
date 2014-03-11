using System;
using System.IO;
using System.Runtime.InteropServices;
using SHDocVw;


namespace ABC.Applications.Explorer
{
	// TODO: At some point, some more verified/wrapped version of this could possibly be moved to Whathecode.Interop.
	static class Interop
	{
		static readonly Guid SID_STopLevelBrowser = new Guid( 0x4C96BE40, 0x915C, 0x11CF, 0x99, 0xD3, 0x00, 0xAA, 0x00, 0x4A, 0xE8, 0x37 );

		public static byte[] GetPidl( this InternetExplorer explorerWindow )
		{
			// Query for IPersistFolder2 which allows to get information about the currently visible folder.
			// ReSharper disable once SuspiciousTypeConversion.Global
			var serviceProvider = (IServiceProvider)explorerWindow;
			object sb;
			serviceProvider.QueryService( SID_STopLevelBrowser, typeof( IShellBrowser ).GUID, out sb );
			var shellBrowser = (IShellBrowser)sb;
			object fv;
			shellBrowser.QueryActiveShellView( out fv );
			var folderView = (IFolderView)fv;
			object pf;
			folderView.GetFolder( typeof( IPersistFolder2 ).GUID, out pf );
			var persistFolder = (IPersistFolder2)pf;

			// Get current folder pointer to item identifier list (PIDL): http://msdn.microsoft.com/en-us/library/windows/desktop/bb773321(v=vs.85).aspx
			IntPtr pidl;
			persistFolder.GetCurFolder( out pidl );

			// Write the PIDL to a byte array: http://msdn.microsoft.com/en-us/library/windows/desktop/cc144090(v=vs.85).aspx
			var byteStream = new MemoryStream();
			var bytes = new BinaryWriter( byteStream );
			IntPtr processPidl = pidl;
			ushort nextLength;
			do
			{
				// Read length.
				var readLength = new short[ 1 ];
				Marshal.Copy( processPidl, readLength, 0, 1 );
				nextLength = (ushort)readLength[ 0 ];

				// Read identifier.
				if ( nextLength != 0 )
				{
					byte[] identifier = new byte[ nextLength ];
					Marshal.Copy( processPidl, identifier, 0, nextLength );
					bytes.Write( identifier );
				}

				processPidl += nextLength;
			}
			while ( nextLength != 0 );
			bytes.Write( new byte[] { 0, 0 } ); // 2-byte NULL terminator.

			Marshal.FreeCoTaskMem( pidl );

			return byteStream.ToArray();
		}
	}


	[InterfaceType( ComInterfaceType.InterfaceIsIUnknown ), Guid( "6D5140C1-7436-11CE-8034-00AA006009FA" )]
	interface IServiceProvider
	{
		void QueryService(
			[MarshalAs( UnmanagedType.LPStruct )] Guid guidService,
			[MarshalAs( UnmanagedType.LPStruct )] Guid riid,
			[MarshalAs( UnmanagedType.IUnknown )] out object ppvObject );
	}


	[InterfaceType( ComInterfaceType.InterfaceIsIUnknown ), Guid( "1AC3D9F0-175C-11d1-95BE-00609797EA4F" )]
	interface IPersistFolder2
	{
		void GetClassID( out Guid pClassID );
		void Initialize( IntPtr pidl );
		void GetCurFolder( out IntPtr pidl );
	}


	[InterfaceType( ComInterfaceType.InterfaceIsIUnknown ), Guid( "000214E2-0000-0000-C000-000000000046" )]
	interface IShellBrowser
	{
		void _VtblGap0_12(); // Skip 12 members.
		void QueryActiveShellView( [MarshalAs( UnmanagedType.IUnknown )] out object ppshv );
		// The rest is not defined.
	}


	[InterfaceType( ComInterfaceType.InterfaceIsIUnknown ), Guid( "cde725b0-ccc9-4519-917e-325d72fab4ce" )]
	interface IFolderView
	{
		void _VtblGap0_2(); // Skip 2 members.
		void GetFolder( [MarshalAs( UnmanagedType.LPStruct )] Guid riid, [MarshalAs( UnmanagedType.IUnknown )] out object ppv );
		// The rest is not defined.
	}
}
