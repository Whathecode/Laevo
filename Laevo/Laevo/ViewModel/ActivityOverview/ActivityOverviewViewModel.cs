using System.Collections.Generic;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview.Binding;
using VirtualDesktopManager;
using Whathecode.System.Windows.Aspects.ViewModel;


namespace Laevo.ViewModel.ActivityOverview
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class ActivityOverviewViewModel
	{
		/// <summary>
		///   Event which is triggered when an activity is opened.
		/// </summary>
		public event ActivityViewModel.OpenedActivityEventHandler OpenedActivityEvent;

		readonly Model.Laevo _model;
		readonly DesktopManager _desktopManager = new DesktopManager();
		readonly List<ActivityViewModel> _desktops = new List<ActivityViewModel>();

		public ActivityViewModel CurrentActivityViewModel { get; private set; }


		public ActivityOverviewViewModel( Model.Laevo model )
		{
			_model = model;			

			// Initialize a view model for all activities.
			foreach ( var activity in _model.Activities )
			{
				ActivityViewModel viewModel;
				if ( _model.CurrentActivity == activity )
				{
					// Ensure current (first) activity is assigned to the correct desktop.
					viewModel = new ActivityViewModel( activity, _desktopManager, _desktopManager.CurrentDesktop );
					CurrentActivityViewModel = viewModel;
				}
				else
				{
					viewModel = new ActivityViewModel( activity, _desktopManager );
				}

				viewModel.OpenedActivityEvent += OnActivityOpened;
				_desktops.Add( viewModel );
			}				
		}

		~ActivityOverviewViewModel()
		{
			_desktopManager.Close();
		}

		void OnActivityOpened( ActivityViewModel viewModel )
		{
			CurrentActivityViewModel = viewModel;

			if ( OpenedActivityEvent != null )
			{
				OpenedActivityEvent( viewModel );
			}
		}
	}
}