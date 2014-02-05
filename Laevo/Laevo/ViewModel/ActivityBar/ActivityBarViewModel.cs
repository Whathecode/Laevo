using System.Collections.ObjectModel;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityBar.Binding;
using Laevo.ViewModel.ActivityOverview;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;


namespace Laevo.ViewModel.ActivityBar
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class ActivityBarViewModel : AbstractViewModel
	{
		[NotifyProperty( Binding.Properties.HomeActivity )]
		public ActivityViewModel HomeActivity { get; private set; }

		/// <summary>
		/// List representing currently all opened activities and current one, which is always on the first position.
		/// TODO: Ideally this should be a read only collection.
		/// </summary>
		[NotifyProperty( Binding.Properties.OpenPlusCurrentActivities )]
		public ObservableCollection<ActivityViewModel> OpenPlusCurrentActivities { get; private set; }

		/// <summary>
		/// Current activated activity.
		/// </summary>
		[NotifyProperty( Binding.Properties.CurrentActivity )]
		public ActivityViewModel CurrentActivity { get; private set; }

		/// <summary>
		/// Currently selected activity.
		/// </summary>
		[NotifyProperty( Binding.Properties.SelectedActivity )]
		public ActivityViewModel SelectedActivity { get; private set; }


		public ActivityBarViewModel( ActivityOverviewViewModel overview )
		{
			HomeActivity = overview.HomeActivity;
			CurrentActivity = overview.CurrentActivityViewModel;

			OpenPlusCurrentActivities = new ObservableCollection<ActivityViewModel> { HomeActivity };

			overview.ActivatedActivityEvent += OnActivityActivated;
			overview.RemovedActivityEvent += OnActivityRemoved;
			overview.OpenedActivityEvent += OnOpenedActivityEvent;
			overview.StoppedActivityEvent += OnStoppedActivityEvent;
		}


		void OnActivityActivated( ActivityViewModel oldActivity, ActivityViewModel newActivity )
		{
			CurrentActivity = newActivity;

			if ( oldActivity != newActivity )
			{
				if ( oldActivity != null && !oldActivity.IsOpen )
				{
					OpenPlusCurrentActivities.Remove( oldActivity );
				}

				// Checks if new activity is in the list, if no adds it on front, if yes changes its positoin to first- behavior to simulate windows alt+tab switching.
				int newActivityIndex = OpenPlusCurrentActivities.IndexOf( newActivity );
				if ( newActivity != null && newActivityIndex == -1 )
				{
					OpenPlusCurrentActivities.Insert( 0, newActivity );
				}
				else if ( newActivityIndex != -1 )
				{
					OpenPlusCurrentActivities.Move( newActivityIndex, 0 );
				}
			}
		}

		void OnActivityRemoved( ActivityViewModel removed )
		{
			OpenPlusCurrentActivities.Remove( removed );
		}

		void OnOpenedActivityEvent( ActivityViewModel opened )
		{
			if ( !OpenPlusCurrentActivities.Contains( opened ) )
			{
				OpenPlusCurrentActivities.Add( opened );
			}
		}

		void OnStoppedActivityEvent( ActivityViewModel stopped )
		{
			OpenPlusCurrentActivities.Remove( stopped );
		}

		int _selectionIndex;
		/// <summary>
		/// Selects next activity.
		/// </summary>
		internal void SelectNextActivity()
		{
			if ( OpenPlusCurrentActivities.Count > 1 )
			{
				_selectionIndex++;

				// Go back to the beginning when selection index is outside of the collection to traverse.
				if ( _selectionIndex == OpenPlusCurrentActivities.Count )
				{
					_selectionIndex = 0;
				}

				SelectedActivity = OpenPlusCurrentActivities[ _selectionIndex ];
			}
		}

		/// <summary>
		/// Activates selected activity.
		/// </summary>
		internal void ActivateSelectedActivity()
		{
			if ( SelectedActivity != null )
			{
				SelectedActivity.ActivateActivity( false );
				SelectedActivity = null;
			}
			_selectionIndex = 0;
		}

		protected override void FreeUnmanagedResources()
		{
			// Nothing to do.
		}

		public override void Persist()
		{
			// Nothing to do.
		}
	}
}