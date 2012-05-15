namespace Laevo.View.Activity
{
	/// <summary>
	/// Interaction logic for EditActivityPopup.xaml
	/// </summary>
	public partial class EditActivityPopup
	{
		public EditActivityPopup()
		{
			InitializeComponent();
		}

		void OnCloseButtonClicked( object sender, System.Windows.RoutedEventArgs e )
		{
			Close();
		}
	}
}
