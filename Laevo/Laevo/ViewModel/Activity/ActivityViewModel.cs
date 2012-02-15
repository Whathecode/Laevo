using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Laevo.ViewModel.Activity.Binding;
using Microsoft.WindowsAPICodePack.Shell;
using VirtualDesktopManager;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.Activity
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class ActivityViewModel
	{
		readonly static object StaticLock = new object();

		/// <summary>
		///   Path of the folder which contains the file libraries.
		/// </summary>
		const string LibraryName = "Activity Context";

		/// <summary>
		///   The extension of microsoft libraries.
		/// </summary>
		const string LibraryExtension = "library-ms";


		public delegate void OpenedActivityEventHandler( ActivityViewModel viewModel );
		/// <summary>
		///   Event which is triggered when an activity is opened.
		/// </summary>
		public event OpenedActivityEventHandler OpenedActivityEvent;

		readonly Model.Activity _activity;
		readonly DesktopManager _desktopManager;
		readonly VirtualDesktop _virtualDesktop;


		public ActivityViewModel( Model.Activity activity, DesktopManager desktopManager )
			: this( activity, desktopManager, desktopManager.CreateEmptyDesktop() ) { }

		public ActivityViewModel( Model.Activity activity, DesktopManager desktopManager, VirtualDesktop virtualDesktop )
		{
			_activity = activity;
			_desktopManager = desktopManager;
			_virtualDesktop = virtualDesktop;

			InitializeLibrary();
		}


		[CommandExecute( Commands.OpenActivity )]
		public void OpenActivity()
		{
			_desktopManager.SwitchToDesktop( _virtualDesktop );

			InitializeLibrary();

			if ( OpenedActivityEvent != null )
			{
				OpenedActivityEvent( this );
			}
		}

		[CommandExecute( Commands.OpenActivityLibrary )]
		public void OpenActivityLibrary()
		{
			string folderName = Path.Combine( ShellLibrary.LibrariesKnownFolder.Path, LibraryName );
			Process.Start( "explorer.exe", Path.ChangeExtension( folderName, LibraryExtension ) );
		}

		/// <summary>
		///   Initialize the library which contains all the context files.
		/// </summary>
		void InitializeLibrary()
		{
			// Initialize on a separate thread so the UI doesn't lock.		
			var dataPaths = _activity.DataPaths.Select( p => p.AbsolutePath ).ToArray();
			var initializeShellLibrary = new Thread( () =>
			{
				lock ( StaticLock )
				{
					var activityContext = new ShellLibrary( LibraryName, true );
					Array.ForEach( dataPaths, activityContext.Add );
					activityContext.Close();
				}
			} );
			initializeShellLibrary.Start();
		}
	}
}
