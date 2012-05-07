namespace Laevo.View.Settings
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}


		private void OnCloseButtonClicked( object sender, System.Windows.RoutedEventArgs e )
		{
			Close();
		}
	}
}
