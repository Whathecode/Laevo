using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Laevo.Model;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.User;


namespace Laevo.Data.View
{
	/// <summary>
	///   Provides access to the persisted view data of Laevo.
	/// </summary>
	/// <author>Steven Jeuris</author>
	public interface IViewRepository
	{
		UserViewModel User { get; }
		ActivityViewModel Home { get; set; }
		ReadOnlyObservableCollection<ActivityViewModel> Activities { get; }
		ReadOnlyObservableCollection<ActivityViewModel> Tasks { get; }

		ActivityViewModel LoadActivity( Activity activity );
		void LoadActivities( Activity parentActivity );
		void LoadPersonalActivities();
		/// <summary>
		///   Gets the full path (parent activities) of this activity.
		/// </summary>
		/// <param name="activity">The activity to get the path for.</param>
		/// <returns>An ordered list of parent activities, where the last activity is the closest parent.</returns>
		/// <exception cref="InvalidOperationException">Thrown when activity is not within the repository.</exception>
		List<ActivityViewModel> GetPath( ActivityViewModel activity );
		/// <summary>
		///   Add an existing activity, not yet managed by this repository, as a subactivity of the given activity parent.
		///   When no parent activity is specified, the activity is added as a subactivity of <see cref="Home" />.
		/// </summary>
		/// <param name="activity">The existing activity.</param>
		/// <param name="toParent">The parent activity to which to add this subactivity.</param>
		void AddActivity( ActivityViewModel activity, ActivityViewModel toParent = null );
		void RemoveActivity( ActivityViewModel activity );
		void MoveActivity( ActivityViewModel activity, ActivityViewModel toParent );
		/// <summary>
		///   Ensures the activity is located in the right observable collection (Tasks or Activities).
		/// </summary>
		void UpdateActivity( ActivityViewModel activity );

		void SwapTaskOrder( ActivityViewModel task1, ActivityViewModel task2 );

		UserViewModel GetUser( User user );

		void SaveChanges();
	}
}
