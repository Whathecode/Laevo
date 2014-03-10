namespace Laevo.ViewModel.Activity.Binding
{
	public enum Properties
	{
		ActiveTimeSpans,
		ShowActiveTimeSpans,
		Icon,
		Color,
		Label,
		PossibleColors,
		PossibleIcons,
		IsActive,
		IsOpen,
		HasOpenWindows,
		IsSuspended,
		HasUnattendedInterruptions,
		IsPlannedActivity,
		IsEditable,
		LinkedActivities
	}

	public enum Commands
	{
		ActivateActivity,
		OpenActivityLibrary,
		SelectActivity,
		EditActivity,
		OpenActivity,
		StopActivity,
		SuspendActivity,
		ForceSuspend,
		Remove,
		ChangeColor,
		ChangeIcon
	}
}
