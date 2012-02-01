using Laevo.ViewModel.ActivityOverview.Binding;
using VirtualDesktopManager;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.ActivityOverview
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class ActivityOverviewViewModel
	{
		public delegate void OpenedActivityEventHandler();
		/// <summary>
		///   Event which is triggered when an activity is opened.
		/// </summary>
		public event OpenedActivityEventHandler OpenedActivityEvent;

		readonly DesktopManager _desktopManager = new DesktopManager();
		readonly VirtualDesktop _initialDesktop;
		readonly VirtualDesktop _desktop2;
		readonly VirtualDesktop _desktop3;
		readonly VirtualDesktop _desktop4;


		public ActivityOverviewViewModel()
		{
			_initialDesktop = _desktopManager.CurrentDesktop;
			_desktop2 = _desktopManager.CreateEmptyDesktop();
			_desktop3 = _desktopManager.CreateEmptyDesktop();
			_desktop4 = _desktopManager.CreateEmptyDesktop();		
		}


		[CommandExecute( Commands.OpenActivity )]
		public void OpenActivity( string desktopId )
		{
			switch ( int.Parse( desktopId ) )
			{
				case 1:
					_desktopManager.SwitchToDesktop( _initialDesktop );
					break;
				case 2:
					_desktopManager.SwitchToDesktop( _desktop2 );
					break;
				case 3:
					_desktopManager.SwitchToDesktop( _desktop3 );
					break;
				case 4:
					_desktopManager.SwitchToDesktop( _desktop4 );
					break;
			}

			if ( OpenedActivityEvent != null )
			{
				OpenedActivityEvent();
			}
		}
	}
}