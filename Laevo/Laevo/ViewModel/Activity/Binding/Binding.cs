namespace Laevo.ViewModel.Activity.Binding
{
	public enum Properties
	{
		Occurance,
		TimeSpan,
		ActiveTimeSpans,
		ShowActiveTimeSpans,
		Icon,
		Color,
		Label,
		HeightPercentage,
		OffsetPercentage,
		PossibleColors,
		PossibleIcons,
		IsActive,
		IsOpen,
		HasOpenWindows,
		IsSuspended,
		HasUnattendedInterruptions,
		IsPlannedActivity,
		IsEditable
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
