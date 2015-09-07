namespace Laevo.ViewModel.Activity.Binding
{
	public enum Properties
	{
		Icon,
		Color,
		Label,
		PossibleColors,
		PossibleIcons,
		IsActive,
		IsOpen,
		IsToDo,
		IsPlanned,
		ContainsHistory,
		IsSuspended,
		NeedsSuspension,
		HasUnattendedInterruptions,
		IsEditable,
		IsAccessible,
		WorkIntervals,
		OpenInterval,
		AccessUsers,
		ClaimedOwnership,
		OwnedUsers
	}

	public enum Commands
	{
		ActivateActivity,
		OpenActivityLibrary,
		SelectActivity,
		EditActivity,
		OpenActivity,
		OpenTimeLine,
		StopActivity,
		SuspendActivity,
		ForceSuspend,
		Remove,
		MakeToDo,
		RemovePlanning,
		ChangeColor,
		ChangeIcon,
		OpenTimeLineSharing
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
		HasMoreRecentRepresentation,
		ActiveTimeSpans,
		ShowActiveTimeSpans
	}

	public enum WorkIntervalCommands
	{
		EditPlannedInterval
	}
}
