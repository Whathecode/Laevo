using System;


namespace Laevo.ViewModel.ActivityOverview
{
	[Flags]
	public enum Mode
	{
		/// <summary>
		///   Activities can be activated on the overview.
		/// </summary>
		Activate = 1,
		/// <summary>
		///   Activity time lines (subactivities) can be opened.
		/// </summary>
		Hierarchies = 1 << 1,
		/// <summary>
		///   No activity is currently active, and the user needs to select an activity on the overview in order to navigate away from it.
		/// </summary>
		Select = 1 << 2,
		/// <summary>
		///   Pop-up dialog window is shown, user needs to close it firstly to perform any interaction with the time line.
		/// </summary>
		Inactive = 1 << 3,
		/// <summary>
		///   An activity is being suspended.
		/// </summary>
		Suspending = 1 << 4
	}
}
