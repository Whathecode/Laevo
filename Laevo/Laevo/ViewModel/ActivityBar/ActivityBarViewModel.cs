using System.Collections.ObjectModel;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityBar.Binding;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;


namespace Laevo.ViewModel.ActivityBar
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class ActivityBarViewModel : AbstractViewModel
	{
		int _selectionIndex = 1;
		public View.ActivityBar.ActivityBar ActivityBar { get; set; }

		/// <summary>
		/// Home activity.
		/// </summary>
		[NotifyProperty( Binding.Properties.HomeActivity )]
		public ActivityViewModel HomeActivity { get; set; }

		/// <summary>
		/// List representing currently all opened activities and current one, which is always on the first position.
		/// </summary>
		[NotifyProperty( Binding.Properties.OpenPlusCurrentActivities )]
		public ObservableCollection<ActivityViewModel> OpenPlusCurrentActivities { get; set; }

		/// <summary>
		/// Current activity.
		/// </summary>
		[NotifyProperty( Binding.Properties.CurrentActivity )]
		public ActivityViewModel CurrentActivity { get; set; }

		/// <summary>
		/// Current activity.
		/// </summary>
		[NotifyProperty( Binding.Properties.SelectedActivity )]
		public ActivityViewModel SelectedActivity { get; set; }

		public ActivityBarViewModel()
		{
			OpenPlusCurrentActivities = new ObservableCollection<ActivityViewModel>();
		}

		/// <summary>
		/// Selects next activity.
		/// </summary>
		internal void SelectNextActivity()
		{
			if ( OpenPlusCurrentActivities.Count > 1 )
			{
				// Come back on the beginning when selection index is outside of open plus active activities collection.
				if ( _selectionIndex == OpenPlusCurrentActivities.Count )
				{
					_selectionIndex = 0;
				}

				SelectedActivity = OpenPlusCurrentActivities[ _selectionIndex ];
				ActivityBar.SelectNextActivity( _selectionIndex );

				_selectionIndex++;
			}
		}

		/// <summary>
		/// Activates selected activity.
		/// </summary>
		internal void ActivateSelectedActivity()
		{
			if ( SelectedActivity != null )
			{
				// Move the focus from Activity icon after user select one in order to finnish selection action.
				ActivityBar.PassFocusToPreviousItem();

				SelectedActivity.ActivateActivity( false );
				SelectedActivity = null;
			}
			_selectionIndex = 1;
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