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
		IsPlanned,
		HasOpenWindows,
		IsSuspended,
		NeedsSuspension,
		HasUnattendedInterruptions,
		IsEditable,
		WorkIntervals
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
		RemovePlanning,
		ChangeColor,
		ChangeIcon
	}

	public enum WorkIntervalProperties
	{
		Occurance,
		TimeSpan,
		HeightPercentage,
		OffsetPercentage,
		BaseActivity,
		Position,
		IsPlanned,
		HasMoreRecentRepresentation
	}

	public enum WorkIntervalCommands
	{
		EditPlannedInterval
	}
}
