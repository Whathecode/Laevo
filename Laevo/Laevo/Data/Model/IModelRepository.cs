using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ABC.Interruptions;
using Laevo.Model;
using Laevo.Model.AttentionShifts;


namespace Laevo.Data.Model
{
	/// <summary>
	///   Provides access to the persisted model data of Laevo.
	/// </summary>
	/// <author>Steven Jeuris</author>
	public interface IModelRepository
	{
		User User { get; }

		ReadOnlyCollection<AbstractAttentionShift> AttentionShifts { get; }

		Activity HomeActivity { get; }

		Settings Settings { get; }

		/// <summary>
		///   Get the subactivities of a specified parent activity, or the subactivities of <see cref="HomeActivity" /> when null.
		/// </summary>
		/// <param name="parent">The parent activity to get subactivities of.</param>
		IEnumerable<Activity> GetActivities( Activity parent = null );

		/// <summary>
		///   Returns a list of all activities which are shared.
		/// </summary>
		IEnumerable<Activity> GetSharedActivities();

		/// <summary>
		///   Gets the full path (parent activities) of this activity.
		/// </summary>
		/// <param name="activity">The activity to get the path for.</param>
		/// <returns>An ordered list of parent activities, where the last activity is the closest parent.</returns>
		List<Activity> GetPath( Activity activity );

		/// <summary>
		///   Gets a full list of all unattended interruptions per activity.
		/// </summary>
		Dictionary<Activity, List<AbstractInterruption>> GetUnattendedInterruptions();

		/// <summary>
		///   Create a new activity with the specified name, and add it as a subactivity of the given activity.
		/// </summary>
		/// <param name="name">Name for the newly created activity.</param>
		/// <param name="parent">The parent activity to which to add this subactivity, or null when a root.</param>
		/// <returns>The newly created activity.</returns>
		Activity CreateNewActivity( string name, Activity parent = null );

		/// <summary>
		///   Add an existing activity, not yet managed by this repository, as a subactivity of the given activity parent.
		///   When no parent activity is specified, the activity is added as a subactivity of <see cref="HomeActivity" />.
		/// </summary>
		/// <param name="newActivity">The existing activity.</param>
		/// <param name="parent">The parent activity to which to add this subactivity.</param>
		/// <exception cref="InvalidOperationException">Thrown when an activity which already exists within the repository is added.</exception>
		void AddActivity( Activity newActivity, Activity parent = null );

		/// <summary>
		///   Remove a specified activity, and all of its sub activities.
		/// </summary>
		/// <param name="activity">The activity to remove.</param>
		void RemoveActivity( Activity activity );

		/// <summary>
		///   Moves the specified activity to the specified destination activity.
		/// </summary>
		/// <param name="activity">The activity to move.</param>
		/// <param name="destination">The destination activity, to which <see cref="activity" /> will be added as a subactivity.</param>
		void MoveActivity( Activity activity, Activity destination );

		/// <summary>
		///   Verifies whether the repository already manages the specified activity.
		/// </summary>
		/// <param name="activity">The activity to look for.</param>
		/// <returns>True when the given activity is managed by the repository; false otherwise.</returns>
		bool ContainsActivity( Activity activity );

		void SwapTaskOrder( Activity task1, Activity task2 );

		void AddAttentionShift( AbstractAttentionShift attentionShift );

		void SaveChanges();
	}
}
