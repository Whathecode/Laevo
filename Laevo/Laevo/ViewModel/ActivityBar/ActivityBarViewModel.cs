using System.Collections.ObjectModel;
using System.Linq;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityBar.Binding;
using Laevo.ViewModel.ActivityOverview;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;


namespace Laevo.ViewModel.ActivityBar
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class ActivityBarViewModel : AbstractViewModel
	{
		int _selectionIndex;

		[NotifyProperty( Binding.Properties.Overview )]
		public ActivityOverviewViewModel Overview { get; private set; }

		/// <summary>
		/// List representing currently all opened activities and current one, which is always on the first position.
		/// TODO: Ideally this should be a read only collection.
		/// </summary>
		[NotifyProperty( Binding.Properties.OpenPlusCurrentActivities )]
		public ObservableCollection<ActivityViewModel> OpenPlusCurrentActivities { get; private set; }

		/// <summary>
		/// Currently selected activity.
		/// </summary>
		[NotifyProperty( Binding.Properties.SelectedActivity )]
		public ActivityViewModel SelectedActivity { get; private set; }


		public ActivityBarViewModel( ActivityOverviewViewModel overview )
		{
			Overview = overview;
			OpenPlusCurrentActivities = new ObservableCollection<ActivityViewModel> { overview.HomeActivity };
			overview.Activities.Where( a => a.NeedsSuspension && !a.Equals( overview.HomeActivity ) ).ForEach( OpenPlusCurrentActivities.Add );

			// TODO: Better decoupling from ActivityOverviewViewModel. These events could be provided as services through a central mechanism. Perhaps they don't belong in overview either way.
			overview.OpenedActivityEvent += opened =>
			{
				if ( !opened.IsActive )
				{
					OpenPlusCurrentActivities.Add( opened );
				}
			}; 
			overview.RemovedActivityEvent += removed => OpenPlusCurrentActivities.Remove( removed );
			overview.StoppedActivityEvent += stopped =>
			{
				if ( !stopped.NeedsSuspension )
				{
					OpenPlusCurrentActivities.Remove( stopped );
				}
			};
			overview.ActivatedActivityEvent += OnActivityActivated;
			
			// Activities which are activated are shown in the list until they are suspended.
			overview.SuspendingActivityEvent += model =>
			{
				if ( model != null )
				{
					OpenPlusCurrentActivities.Remove( model );
				}
			};
		}


		void OnActivityActivated( ActivityViewModel oldActivity, ActivityViewModel activatedActivity )
		{
			// When an activity is activated, selection stops.
			SelectedActivity = null;
			_selectionIndex = 0;

			// Nothing to do when activity didn't change.
			if ( oldActivity == activatedActivity )
			{
				return;
			}

			// Similar behavior as windows Alt+Tab window switching.
			// The activated activity is always in front, the previously activated activity is second.
			if ( activatedActivity != null )
			{
				if ( OpenPlusCurrentActivities.Contains( activatedActivity ) )
				{
					OpenPlusCurrentActivities.Move( OpenPlusCurrentActivities.IndexOf( activatedActivity ), 0 );
				}
				else
				{
					// Non-open but activated activites are added to the list.
					OpenPlusCurrentActivities.Insert( 0, activatedActivity );
				}
			}
		}

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

		internal void ActivateSelectedActivity()
		{
			if ( SelectedActivity != null )
			{
				SelectedActivity.ActivateActivity( false );
			}
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