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
				if ( !_unresponsiveWindows.ContainsKey( processName ) )
				{
					UnresponsiveWindows.Add( processName + " (process id: " + processId + ")" );
					_unresponsiveWindows.Add( processName + " (process id: " + processId + ")", w );
				}
			} );
			Desktop = desktop;

			SetSelection();
		}

		void RemoveSelected()
		{
			// Local copy of selectedItems has to be create because filed is updated both from interface and code.
			var selectedItemsLocal = new List<string>( SelectedItems );

			selectedItemsLocal.ForEach( myUnresponsive1 => UnresponsiveWindows.Remove( myUnresponsive1 ) );
		}

		[CommandExecute( Commands.IgnoreSingle )]
		public void IgnoreSingle()
		{
			_unresponsiveWindows.ForEach( window => SelectedItems.ForEach( unresponsive =>
			{
				if ( window.Key == unresponsive )
				{
					window.Value.Ignore = true;
				}
			} ) );
			
			RemoveSelected();
			SetSelection();
		}

		[CommandExecute( Commands.KeepSingle )]
		public void KeepSingle()
		{
			RemoveSelected();
			SetSelection();
		}

		void SetSelection()
		{
			if ( UnresponsiveWindows.Count > 0 )
			{
				SelectedApplicationIndex = UnresponsiveWindows.Count - 1;
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