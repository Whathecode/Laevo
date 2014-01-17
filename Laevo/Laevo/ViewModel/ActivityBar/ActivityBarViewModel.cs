using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityBar.Binding;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.ActivityBar
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class ActivityBarViewModel : AbstractViewModel
	{
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

		public ActivityBarViewModel()
		{
			OpenPlusCurrentActivities = new ObservableCollection<ActivityViewModel>();
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