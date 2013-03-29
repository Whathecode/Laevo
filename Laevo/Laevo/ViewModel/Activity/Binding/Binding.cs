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
		IsPlannedActivity
	}

	public enum Commands
	{
		ActivateActivity,
		OpenActivityLibrary,
		SelectActivity,
		EditActivity,
		OpenActivity,
		CloseActivity,
		Remove,
		ChangeColor,
		ChangeIcon
	}
}
