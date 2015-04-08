﻿using System.Collections.Generic;
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
		ReadOnlyCollection<AbstractAttentionShift> AttentionShifts { get; }

		Activity HomeActivity { get; }

		Settings Settings { get; }

		/// <summary>
		///   Get the subactivities of a specified parent activity, or the subactivities of <see cref="HomeActivity" /> when null.
		/// </summary>
		/// <param name="parent">The parent activity to get subactivities of.</param>
		IEnumerable<Activity> GetActivities( Activity parent = null );

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
		///   When no parent activity is specified, the activity is added as a subactivity of <see cref="HomeActivity" />.
		/// </summary>
		/// <param name="name">Name for the newly created activity.</param>
		/// <param name="parent">The parent activity to which to add this subactivity.</param>
		/// <returns>The newly created activity.</returns>
		Activity CreateNewActivity( string name, Activity parent = null );

		/// <summary>
		///   Remove a specified activity.
		/// </summary>
		/// <param name="activity">The activity to remove.</param>
		void RemoveActivity( Activity activity );

		void SwapTaskOrder( Activity task1, Activity task2 );

		void AddAttentionShift( AbstractAttentionShift attentionShift );

		void SaveChanges();
	}
}
