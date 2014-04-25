using Laevo.ViewModel.Activity.LinkedActivity;


namespace Laevo.View.Activity
{
	public enum LinkedActivityDragOption
	{
		Reschedule,
		ToDoCreate,
		None
	}

	class DraggedLinkedActivity
	{
		public DraggedLinkedActivity( LinkedActivityViewModel linkedActivity, LinkedActivityDragOption dragOption )
		{
			LinkedActivity = linkedActivity;
			DragOption = dragOption;
		}

		public LinkedActivityViewModel LinkedActivity { get; private set; }
		public LinkedActivityDragOption DragOption { get; private set; }
	}
}
