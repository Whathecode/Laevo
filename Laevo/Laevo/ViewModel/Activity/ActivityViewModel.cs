using System.IO;
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
		/// <summary>
		///   Path of the folder which contains the file libraries.
		/// </summary>
		private const string LibraryName = "Activity Context";


		public delegate void OpenedActivityEventHandler();
		/// <summary>
		///   Event which is triggered when an activity is opened.
		/// </summary>
		public event OpenedActivityEventHandler OpenedActivityEvent;

		readonly Model.Activity _activity;
		readonly DesktopManager _desktopManager;
		readonly VirtualDesktop _virtualDesktop;


		static ActivityViewModel()
		{
			// Initialize the library which contains all the context files.
			new ShellLibrary( LibraryName, true ).Close();
		}

		public ActivityViewModel( Model.Activity activity, DesktopManager desktopManager )
			: this( activity, desktopManager, desktopManager.CreateEmptyDesktop() ) { }

		public ActivityViewModel( Model.Activity activity, DesktopManager desktopManager, VirtualDesktop virtualDesktop )
		{
			_activity = activity;
			_desktopManager = desktopManager;
			_virtualDesktop = virtualDesktop;
		}


		[CommandExecute( Commands.OpenActivity )]
		public void OpenActivity()
		{
			_desktopManager.SwitchToDesktop( _virtualDesktop );

			if ( OpenedActivityEvent != null )
			{
				OpenedActivityEvent();
			}
		}
	}
}
