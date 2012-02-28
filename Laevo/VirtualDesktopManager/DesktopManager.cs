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
	/// <license>
	///   This file is part of VirtualDesktopManager.
	///   VirtualDesktopManager is free software: you can redistribute it and/or modify
	///   it under the terms of the GNU General Public License as published by
	///   the Free Software Foundation, either version 3 of the License, or
	///   (at your option) any later version.
	///
	///   VirtualDesktopManager is distributed in the hope that it will be useful,
	///   but WITHOUT ANY WARRANTY; without even the implied warranty of
	///   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	///   GNU General Public License for more details.
	///
	///   You should have received a copy of the GNU General Public License
	///   along with VirtualDesktopManager.  If not, see <http://www.gnu.org/licenses/>.
	/// </license>
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
