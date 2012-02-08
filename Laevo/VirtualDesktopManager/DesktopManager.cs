using System;
using System.Collections.Generic;
using System.Linq;
using Whathecode.System.Collections.Generic;
using Whathecode.System.Windows.Interop;


namespace VirtualDesktopManager
{
	/// <summary>
	///   Allows creating and switching between different <see cref="VirtualDesktop" />'s.
	/// </summary>
	public class DesktopManager
	{
		/// <summary>
		///   A list of processes with associated window classes which should be ignored by the desktop manager.
		/// </summary>
		static readonly TupleList<string, string> IgnoreProcesses = new TupleList<string, string>
		{
			// Format: { process name, class name }
			{ "explorer", "Button" },			// Start button.
			{ "explorer", "Shell_TrayWnd" },	// Start bar.
			{ "explorer", "Progman" }			// Desktop icons.
		};

		readonly List<WindowInfo> _ignoreWindows;
		readonly List<VirtualDesktop> _availableDesktops = new List<VirtualDesktop>();

		public VirtualDesktop CurrentDesktop { get; private set; }


		public DesktopManager()
		{
			// Determine which windows shouldn't be managed by the desktop manager.
			_ignoreWindows = WindowManager.GetWindows().Where( w => !IsValidWindow( w ) ).ToList();

			CurrentDesktop = new VirtualDesktop( GetOpenWindows() );
			_availableDesktops.Add( CurrentDesktop );
		}


		/// <summary>
		///   Create an empty virtual desktop with no windows assigned to it.
		/// </summary>
		/// <returns>The newly created virtual desktop.</returns>
		public VirtualDesktop CreateEmptyDesktop()
		{
			var newDesktop = new VirtualDesktop();
			_availableDesktops.Add( newDesktop );

			return newDesktop;
		}

		/// <summary>
		///   Switch to the given virtual desktop.
		/// </summary>
		/// <param name="desktop">The desktop to switch to.</param>
		public void SwitchToDesktop( VirtualDesktop desktop )
		{
			if ( CurrentDesktop == desktop )
			{
				return;
			}

			// Update which windows are associated to the current virtual desktop.
			IEnumerable<WindowInfo> newWindows = GetOpenWindows()
				.Except( _availableDesktops.SelectMany( d => d.Windows ) )
				.Where( IsValidWindow );
			CurrentDesktop.UpdateWindows( newWindows );

			// Hide windows and show those from the new desktop.
			CurrentDesktop.Hide();
			desktop.Show();

			CurrentDesktop = desktop;
		}

		/// <summary>
		///   Closes the virtual desktop manager by restoring all windows.
		/// </summary>
		public void Close()
		{
			_availableDesktops.ForEach( d => d.Show() );
		}

		static bool IsValidWindow( WindowInfo window )
		{
			// TODO: Remove test stuff.
			if ( window.GetClassName() == "Progman" )
			{
				var childWindows = window.GetChildWindows();
				var iconsWindow = childWindows.First( w => w.GetClassName() == "SysListView32" );
			}

			return
				window.IsVisible() &&
				!IgnoreProcesses.Contains( new Tuple<string, string>( window.GetProcess().ProcessName, window.GetClassName() ) );
		}

		IEnumerable<WindowInfo> GetOpenWindows()
		{
			return WindowManager.GetWindows().Except( _ignoreWindows );
		}
	}
}
