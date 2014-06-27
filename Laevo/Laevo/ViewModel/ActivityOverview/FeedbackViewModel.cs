using System.Windows.Media;


namespace Laevo.ViewModel.ActivityOverview
{
	enum FeedbackType
	{
		Smile,
		Frown,
		Question
	};

	class FeedbackViewModel
	{
		public FeedbackType FeedbackType { get; set; }
		public Color Color { get; set; }
		public string SendText { get; set; }
		public string FeedbackText { get; set; }
		public string FeedbackTextHint { get; set; }
		public string EmailTextHint { get; set; }
		public ImageSource Image { get; set; }
	}
}