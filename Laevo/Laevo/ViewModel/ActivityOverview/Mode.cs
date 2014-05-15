using System;


namespace Laevo.ViewModel.ActivityOverview
{
	[Flags]
	public enum Mode
	{
		/// <summary>
		///   Activities can be activated on the overview.
		/// </summary>
		Activate = 0,
		/// <summary>
		///   No activity is currently active, and the user needs to select an activity on the overview in order to navigate away from it.
		/// </summary>
		Select = 1,
		/// <summary>
		///   An activity is being edited.
		/// </summary>
		Edit = 2,
		/// <summary>
		///   An activity is being suspended.
		/// </summary>
		Suspending = 4
	}
}
