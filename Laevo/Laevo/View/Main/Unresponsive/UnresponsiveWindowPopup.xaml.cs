using System.Windows.Controls;
using Laevo.ViewModel.Main.Unresponsive;


namespace Laevo.View.Main.Unresponsive
{
	partial class UnresponsiveWindowPopup
	{
		public UnresponsiveWindowPopup()
		{
			InitializeComponent();
			Loaded += ( sender, args ) => UnresponsiveListBox.Focus();
			UnresponsiveListBox.SelectedIndex = 0;

			Activated += ( sender, args ) => UnresponsiveListBox.SelectedIndex = 0;
		}

		void SelectedUnresponsiveChanged( object sender, SelectionChangedEventArgs e )
		{
			// Since ListView component does not support binding to collections, 
			// we have to update it manually.
			var viewModel = (UnresponsiveViewModel)DataContext;
			foreach ( var added in e.AddedItems )
			{
				viewModel.SelectedItems.Add( added.ToString() );
			}
			foreach ( var deleted in e.RemovedItems )
			{
				viewModel.SelectedItems.Remove( deleted.ToString() );
			}
		}

	}
}