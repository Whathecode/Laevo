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
		}

		void SelectedUnresponsiveChanged( object sender, SelectionChangedEventArgs e )
		{
			// Since listView does not support binding to SelectedItems, 
			// we have to update filed manually.
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