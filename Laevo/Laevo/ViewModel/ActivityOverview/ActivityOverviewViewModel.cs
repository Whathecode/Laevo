using System;
using System.Collections.Generic;
using System.Timers;
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
		// TODO: How to reach the desktops?
		public readonly List<ActivityViewModel> _desktops = new List<ActivityViewModel>();

		/// <summary>
		///   Timer used to update data regularly.
		/// </summary>
		readonly Timer _updateTimer = new Timer( 1000 );

		public ActivityViewModel CurrentActivityViewModel { get; private set; }

		[NotifyProperty( Binding.Properties.CurrentTime )]
		public DateTime CurrentTime { get; private set; }


		public ActivityOverviewViewModel( Model.Laevo model )
		{
			_model = model;

			// Hook up timer.
			_updateTimer.Elapsed += UpdateData;
			_updateTimer.Start();

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
			_updateTimer.Stop();
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

		void UpdateData( object sender, ElapsedEventArgs e )
		{
			CurrentTime = DateTime.Now;
		}
	}
}