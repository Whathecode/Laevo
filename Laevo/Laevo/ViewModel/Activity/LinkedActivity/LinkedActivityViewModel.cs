using System;
using System.Collections.ObjectModel;
using System.Linq;
using Laevo.ViewModel.Activity.LinkedActivity.Binding;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.Activity.LinkedActivity
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	public class LinkedActivityViewModel : AbstractViewModel
	{
		/// <summary>
		///   Activity place related to other linked activities.
		/// </summary>
		[NotifyProperty( Binding.Properties.Position )]
		public ActivityPosition Position { get; private set; }

		/// <summary>
		///   The time when the activity started or will start.
		/// </summary>
		[NotifyProperty( Binding.Properties.Occurance )]
		public DateTime Occurance { get; set; }

		[NotifyPropertyChanged( Binding.Properties.Occurance )]
		public void OnOccuranceChanged( DateTime oldOccurance, DateTime newOccurance )
		{
			if ( IsPlanned )
			{
				UpdateLastPlannedInterval( newOccurance, TimeSpan );
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
			if ( IsPlanned )
			{
				UpdateLastPlannedInterval( Occurance, newDuration );
			}
		}

		void UpdateLastPlannedInterval( DateTime atTime, TimeSpan duration )
		{
			var plannedIntervals = BaseActivity.Activity.PlannedIntervals;
			plannedIntervals.Last().Interval = new Interval<DateTime>( atTime, atTime + duration );
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
		public ActivityViewModel BaseActivity { get; private set; }

		[NotifyProperty( Binding.Properties.IsPlanned )]
		public bool IsPlanned { get; set; }

		/// <summary>
		///   Determines whether or not the base activity has a more recent representation elsewhere.
		///   This could be a work interval somewhere in the future, or a to-do item.
		/// </summary>
		[NotifyProperty( Binding.Properties.HasMoreRecentRepresentation )]
		public bool HasMoreRecentRepresentation { get; private set; }


		public LinkedActivityViewModel( ActivityViewModel baseActivity )
		{
			BaseActivity = baseActivity;

			ObservableCollection<LinkedActivityViewModel> intervals = BaseActivity.WorkIntervals;
			intervals.CollectionChanged += ( sender, args ) =>
			{
				// Update Position.
				int index = intervals.IndexOf( this );
				var position = ActivityPosition.None;
				if ( index == 0 )
				{
					position |= ActivityPosition.Start;
				}
				if ( index == intervals.Count - 1 )
				{
					position |= ActivityPosition.End;
				}
				Position = position;

				UpdateHasMoreRecentRepresentation();
			};

			BaseActivity.ToDoChangedEvent += activity => UpdateHasMoreRecentRepresentation();
		}

		void UpdateHasMoreRecentRepresentation()
		{
			ObservableCollection<LinkedActivityViewModel> intervals = BaseActivity.WorkIntervals;
			HasMoreRecentRepresentation =
				BaseActivity.IsToDo ||
				( intervals.Count != 1 && intervals.IndexOf(this) != intervals.Count - 1 );
		}


		[CommandExecute( Commands.EditPlannedInterval )]
		public void EditActivity()
		{
			BaseActivity.EditActivity( true );
		}

		public bool IsPast()
		{
			return Occurance + TimeSpan < DateTime.Now;
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