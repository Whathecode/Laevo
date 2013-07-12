using System.Windows;


namespace ABC.Interruptions.Google
{
	/// <summary>
	/// Interaction logic for CredentialsDialog.xaml
	/// </summary>
	public partial class CredentialsDialog
	{
		public CredentialsDialog()
		{
			InitializeComponent();
		}


		void SaveClicked( object sender, RoutedEventArgs e )
		{
			DialogResult = true;
		}

		void CancelClicked( object sender, RoutedEventArgs e )
		{
			DialogResult = false;
		}
	}
}
