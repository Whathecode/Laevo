using System.Collections.Generic;
using System.Collections.ObjectModel;
using ABC.Workspaces.Windows;
using Laevo.ViewModel.Main.Unresponsive.Binding;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.Main.Unresponsive
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class UnresponsiveViewModel : AbstractViewModel
	{
		public delegate void UnresponsiveEventHandler();

		/// <summary>
		///   Event which is triggered when all unresponsive windows are handled.
		/// </summary>
		public event UnresponsiveEventHandler UnresponsiveHandled;

		[NotifyProperty( Binding.Properties.UnresponsiveWindows )]
		public ObservableCollection<UnresponsiveWindow> UnresponsiveWindows { get; private set; }

		[NotifyProperty( Binding.Properties.SelectedApplication )]
		public int SelectedApplicationIndex { get; set; }

		public List<UnresponsiveWindow> SelectedItems { get; set; }

		public UnresponsiveViewModel( List<WindowSnapshot> windows )
		{
			SelectedItems = new List<UnresponsiveWindow>();
			UnresponsiveWindows = new ObservableCollection<UnresponsiveWindow>();

			windows.ForEach( w =>
			{
				var processName = w.Window.GetProcess().ProcessName;
				var processId = w.Window.GetProcess().Id;

				UnresponsiveWindows.Add( new UnresponsiveWindow( processName, processId, w ) );
			} );

			SetSelectionOrClose();
		}

		void RemoveSelected()
		{
			// SelectedItems are updated from code behind (ListView component collection binding issue) 
			// we have to create a local copy to safely enumerate over list.
			var selectedItemsLocal = new List<UnresponsiveWindow>( SelectedItems );
			selectedItemsLocal.ForEach( selectedItemLocal => UnresponsiveWindows.Remove( selectedItemLocal ) );
		}

		[CommandExecute( Commands.Ignore )]
		public void Ignore()
		{
			SelectedItems.ForEach( unresponsiveWindow => { unresponsiveWindow.WindowSnapshot.Ignore = true; } );

			RemoveSelected();
			SetSelectionOrClose();
		}

		[CommandExecute( Commands.Keep )]
		public void Keep()
		{
			RemoveSelected();
			SetSelectionOrClose();
		}

		void SetSelectionOrClose()
		{
			if ( UnresponsiveWindows.Count > 0 )
			{
				SelectedApplicationIndex = 0;
			}
			else
			{
				UnresponsiveHandled();
			}
		}

		protected override void FreeUnmanagedResources()
		{
			// Nothing to do.
		}

		public override void Persist()
		{
			// Nothing to do.
		}
	}
}