using System.Collections.Generic;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview.Binding;
using VirtualDesktopManager;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
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

		[NotifyProperty( Binding.Properties.Activity1 )]
		public ActivityViewModel Activity1 { get; private set; }
		[NotifyProperty( Binding.Properties.Activity2 )]
		public ActivityViewModel Activity2 { get; private set; }
		[NotifyProperty( Binding.Properties.Activity3 )]
		public ActivityViewModel Activity3 { get; private set; }
		[NotifyProperty( Binding.Properties.Activity4 )]
		public ActivityViewModel Activity4 { get; private set; }


		public ActivityOverviewViewModel( Model.Laevo model )
		{
			_model = model;			

			// Initialize a view model for all activities.
			foreach ( var activity in _model.Activities )
			{
				var viewModel = _model.CurrentActivity != activity	// Ensure current (first) activity is assigned to the correct desktop.
					? new ActivityViewModel( activity, _desktopManager )
					: new ActivityViewModel( activity, _desktopManager, _desktopManager.CurrentDesktop );
				viewModel.OpenedActivityEvent += OnActivityOpened;
				_desktops.Add( viewModel );
			}				

			// TODO: Remove temporary desktops.
			Activity1 = _desktops[ 0 ];
			Activity2 = _desktops[ 1 ];
			Activity3 = _desktops[ 2 ];
			Activity4 = _desktops[ 3 ];
		}

		~ActivityOverviewViewModel()
		{
			_desktopManager.Close();
		}


		void OnActivityOpened()
		{
			if ( OpenedActivityEvent != null )
			{
				OpenedActivityEvent();
			}
		}
	}
}