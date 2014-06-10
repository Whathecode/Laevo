using System.Windows;
using System.Windows.Controls;
using Laevo.Logging;
using NLog;


namespace Laevo.View.ActivityOverview
{
	/// <summary>
	/// Interaction logic for FeedbackWindow.xaml
	/// </summary>
	public partial class FeedbackWindow
	{
		static readonly Logger Log = LogManager.GetCurrentClassLogger();

		public FeedbackWindow()
		{
			InitializeComponent();
		}

		void OnSendButtonClicked( object sender, RoutedEventArgs e )
		{
			var button = (Button)sender;
			var logDatas = new[] { new LogData( "Feedback text", FeedbackTextBox.Text ), new LogData( "Email", EmailTextBox.Text ), new LogData( "Type", button.Content ) };
			Log.InfoWithData( "Feedback sent.", logDatas );
			Close();
		}

		void OnCancelButtonClicked( object sender, RoutedEventArgs e )
		{
			Close();
		}
	}
}