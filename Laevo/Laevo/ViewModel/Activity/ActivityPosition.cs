using System;


namespace Laevo.ViewModel.Activity
{
	/// <summary>
	///   Determines which part of the activity a <see cref="WorkIntervalViewModel" /> shows.
	/// </summary>
	[Flags]
	public enum ActivityPosition
	{
		/// <summary>
		///   The view model does not show the start, nor the end of the activity.
		/// </summary>
		None,
		/// <summary>
		///   The view model shows the start of the activity. This is the first time the activity was opened.
		/// </summary>
		Start,
		/// <summary>
		///   The view model shows the end of the activity. This is the last time the activity was or is open.
		/// </summary>
		End
	};
}