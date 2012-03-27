using System.Windows.Input;


namespace Laevo.View.Activity
{	
	/// <summary>
	/// Interaction logic for ActivityControl.xaml
	/// </summary>
	public partial class ActivityControl
	{
		public static readonly RoutedCommand MouseDragged = new RoutedCommand( "MouseDragged", typeof( ActivityControl ) );


		public ActivityControl()
		{
			InitializeComponent();
		}


		void MoveActivity( object sender, ExecutedRoutedEventArgs e )
		{
			throw new System.NotImplementedException();
		}

		private void CommandBinding_CanExecute( object sender, CanExecuteRoutedEventArgs e )
		{
			e.CanExecute = true;
		}
	}
}
