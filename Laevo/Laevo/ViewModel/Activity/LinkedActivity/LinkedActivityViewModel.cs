using System;
using Laevo.ViewModel.Activity.LinkedActivity.Binding;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;


namespace Laevo.ViewModel.Activity.LinkedActivity
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	public class LinkedActivityViewModel : AbstractViewModel
	{

		/// <summary>
		///   Activity place with refering to other linked activities.
		/// </summary>
		[NotifyProperty( Binding.Properties.Position )]
		public ActivityPosition Position { get; set; }

		/// <summary>
		///   The time when the activity started or will start.
		/// </summary>
		[NotifyProperty( Binding.Properties.Occurance )]
		public DateTime Occurance { get; set; }

		[NotifyPropertyChanged( Binding.Properties.Occurance )]
		public void OnOccuranceChanged( DateTime oldOccurance, DateTime newOccurance )
		{
			if ( BaseActivity.IsPlannedActivity )
			{
				BaseActivity.Activity.UpdateInterval( newOccurance, TimeSpan );
			}
		}

		/// <summary>
		///   The entire timespan during which the activity has been open, regardless of whether it was closed in between.
		/// </summary>
		[NotifyProperty( Binding.Properties.TimeSpan )]
		public TimeSpan TimeSpan { get; set; }

		[NotifyPropertyChanged( Binding.Properties.TimeSpan )]
		public void OnTimeSpanChanged( TimeSpan oldDuration, TimeSpan newDuration )
		{
			if ( BaseActivity.IsPlannedActivity )
			{
				BaseActivity.Activity.UpdateInterval( Occurance, newDuration );
			}
		}

		/// <summary>
		///   The percentage of the available height the activity box occupies.
		/// </summary>
		[NotifyProperty( Binding.Properties.HeightPercentage )]
		public double HeightPercentage { get; set; }

		/// <summary>
		///   The offset, as a percentage of the total available height, where to position the activity box, from the bottom.
		/// </summary>
		[NotifyProperty( Binding.Properties.OffsetPercentage )]
		public double OffsetPercentage { get; set; }

		[NotifyProperty( Binding.Properties.BaseActivity )]
		public ActivityViewModel BaseActivity { get; set; }

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