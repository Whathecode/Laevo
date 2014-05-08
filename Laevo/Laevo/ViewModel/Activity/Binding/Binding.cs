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
		IsToDo,
		HasOpenWindows,
		IsSuspended,
		HasUnattendedInterruptions,
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
		MakeToDo,
		ChangeColor,
		ChangeIcon
	}
}
