using System.Collections.Generic;
using System.Collections.ObjectModel;
using ABC.Workspaces.Windows;
using Laevo.ViewModel.Main.Unresponsive.Binding;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.Main.Unresponsive
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class UnresponsiveViewModel : AbstractViewModel
	{
		public delegate void UnresponsiveEventHandler();

		/// <summary>
		///   Event which is triggered when all unresponsive window are handled.
		/// </summary>
		public event UnresponsiveEventHandler UnresponsiveHandled;

		[NotifyProperty( Binding.Properties.UnresponsiveWindows )]
		public ObservableCollection<string> UnresponsiveWindows { get; set; }

		[NotifyProperty( Binding.Properties.Desktop )]
		public VirtualDesktop Desktop { get; private set; }

		[NotifyProperty( Binding.Properties.SelectedApplication )]
		public int SelectedApplicationIndex { get; set; }

		readonly Dictionary<string, WindowSnapshot> _unresponsiveWindows;
		public List<string> SelectedItems { get; set; }

		public UnresponsiveViewModel( List<WindowSnapshot> windows, VirtualDesktop desktop )
		{
			SelectedItems = new List<string>();

			_unresponsiveWindows = new Dictionary<string, WindowSnapshot>();
			UnresponsiveWindows = new ObservableCollection<string>();

			windows.ForEach( w =>
			{
				var processName = w.Window.GetProcess().ProcessName;
				var processId = w.Window.GetProcess().Id;

				UnresponsiveWindows.Add( processName + " (process id: " + processId + ")" );
				_unresponsiveWindows.Add( processName + " (process id: " + processId + ")", w );
			} );
			Desktop = desktop;

			SetSelectionOrClose();
		}

		void RemoveSelected()
		{
			// SelectedItems are updated from code behind (ListView component collection binding issue) 
			// we have to create a local copy to safely enumerate over list.
			var selectedItemsLocal = new List<string>( SelectedItems );
			selectedItemsLocal.ForEach( selectedItemLocal => UnresponsiveWindows.Remove( selectedItemLocal ) );
		}

		[CommandExecute( Commands.Ignore )]
		public void Ignore()
		{
			_unresponsiveWindows.ForEach( window => SelectedItems.ForEach( unresponsive =>
			{
				if ( window.Key == unresponsive )
				{
					window.Value.Ignore = true;
				}
			} ) );

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