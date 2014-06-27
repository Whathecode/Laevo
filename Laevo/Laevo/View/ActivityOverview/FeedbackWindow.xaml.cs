using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Laevo.Logging;
using Laevo.ViewModel.ActivityOverview;
using NLog;
using Xceed.Wpf.Toolkit;


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
			DataContextChanged += ( sender, args ) =>
			{
				var dataContext = (FeedbackViewModel)DataContext;
				SendButton.IsEnabled = dataContext.FeedbackType != FeedbackType.Question;
			};
		}

		void OnSendButtonClicked( object sender, RoutedEventArgs e )
		{
			var dataContext = (FeedbackViewModel)DataContext;
			var logDatas = new[]
			{ new LogData( "Feedback text", FeedbackTextBox.Text ), new LogData( "Email", EmailTextBox.Text ), new LogData( "Type", dataContext.FeedbackType.ToString() ) };
			Log.InfoWithData( "Feedback sent.", logDatas );
			Hide();
		}

		void OnCancelButtonClicked( object sender, RoutedEventArgs e )
		{
			Hide();
		}

		void EmailChanged( object sender, TextChangedEventArgs e )
		{
			var dataContext = (FeedbackViewModel)DataContext;
			if ( dataContext.FeedbackType != FeedbackType.Question )
			{
				return;
			}
			var textBox = (WatermarkTextBox)sender;
			if ( textBox.Text.Length > 0 )
			{
				SendButton.IsEnabled = EmailIsValid( textBox.Text );
			}
		}

		bool EmailIsValid( string emailaddress )
		{
			return Regex.IsMatch( emailaddress, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$" );
		}
	}
}