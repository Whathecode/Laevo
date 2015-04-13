namespace Laevo.ViewModel.ActivityOverview.Binding
{
	public enum Properties
	{
		CurrentTime,
		IsFocusedTimeBeforeNow,
		FocusedRoundedTime,
		FocusedOffsetPercentage,
		VisibleActivity,
		HomeActivity,
		Activities,
		Tasks,
		Path,
		Mode,
		CurrentActivityViewModel,
		TimeLineRenderScale,
		EnableAttentionLines
	}

	public enum Commands
	{
		NewTask,
		NewActivity,
		PlanActivity,
		OpenHome,
		SwitchPersonalHierarchies,
		OpenUserProfile
	}
}
