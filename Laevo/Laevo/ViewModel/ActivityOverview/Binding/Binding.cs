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
		EnableAttentionLines,
		Notifications,
		UnreadNotificationsCount
	}

	public enum Commands
	{
		NewTask,
		NewActivity,
		PlanActivity,
		OpenHome,
		OpenUserProfile,
		OpenTimeLineSharing,
		OpenNotifications
	}
}
