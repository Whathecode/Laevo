using System.Collections.Generic;
using System.Linq;
using Laevo.ViewModel.ActivityOverview.Binding;
using VirtualDesktopManager;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;
using Laevo.Model;


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

		readonly Model.Laevo _model;
		readonly DesktopManager _desktopManager = new DesktopManager();
		readonly Dictionary<Activity, VirtualDesktop> _desktops = new Dictionary<Activity, VirtualDesktop>();


		public ActivityOverviewViewModel( Model.Laevo model )
		{
			_model = model;

			// Ensure current acitivity is assigned to the correct desktop.
			if ( _model.CurrentActivity != null )
			{
				_desktops.Add( _model.CurrentActivity, _desktopManager.CurrentDesktop );
			}

			// Initialize all remaining activities with an empty desktop.
			_model.Activities
				.Where( a => a != _model.CurrentActivity )
				.ForEach( a => _desktops.Add( a, _desktopManager.CreateEmptyDesktop() ) );
		}

		~ActivityOverviewViewModel()
		{
			_desktopManager.Close();
		}


		[CommandExecute( Commands.OpenActivity )]
		public void OpenActivity( string desktopId )
		{
			// TODO: Pass proper Activity reference instead.
			int desktopNumber = int.Parse( desktopId ) - 1;
			_desktopManager.SwitchToDesktop( _desktops.Values.ElementAt( desktopNumber ) );

			if ( OpenedActivityEvent != null )
			{
				OpenedActivityEvent();
			}
		}
	}
}