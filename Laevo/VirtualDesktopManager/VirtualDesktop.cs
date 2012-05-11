using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Whathecode.System.Windows.Interop;


namespace VirtualDesktopManager
{
	/// <summary>
	///   Represents a virtual desktop.
	/// </summary>
	/// <author>Steven Jeuris</author>
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
	public class VirtualDesktop
	{
		List<WindowInfo> _windows = new List<WindowInfo>();
		public ReadOnlyCollection<WindowInfo> Windows
		{
			get { return _windows.AsReadOnly(); }
		}


		/// <summary>
		///   Create an empty virtual desktop.
		/// </summary>
		internal VirtualDesktop() {}


		/// <summary>
		///   Create a virtual desktop which is initialized with a set of existing windows.
		/// </summary>
		/// <param name="initialWindows">The windows which should belong to the new virtual desktop.</param>
		internal VirtualDesktop( IEnumerable<WindowInfo> initialWindows )
		{
			_windows.AddRange( initialWindows );
		}


		/// <summary>
		///   Adds the passed new windows and removes windows which are no longer open from the list.
		/// </summary>
		/// <param name = "newWindows">Newly opened windows on this virtual desktop.</param>
		public void UpdateWindowAssociations( IEnumerable<WindowInfo> newWindows )
		{
			// Add new windows.
			_windows.AddRange( newWindows );

			// Remove windows which are no longer open.
			_windows = _windows.Where( w => !w.IsDestroyed() ).ToList();
		}

		/// <summary>
		///   Adds the passed window to the virtual desktop and activates it.
		/// </summary>
		/// <param name = "toAdd">The window to add.</param>
		public void AddWindow( WindowInfo toAdd )
		{
			_windows.Add( toAdd );
			toAdd.Show();
		}	

		/// <summary>
		///   Removes the passed window from the virtual desktop and hides it.
		/// </summary>
		/// <param name = "toRemove">The window to remove.</param>
		public void RemoveWindow( WindowInfo toRemove )
		{
			_windows.Remove( toRemove );
			toRemove.Hide();
		}

		/// <summary>
		///   Show all windows associated with this virtual desktop.
		/// </summary>
		public void Show()
		{
			_windows.ForEach( w => w.Show( false ) );
		}

		/// <summary>
		///   Hide all windows associated with this virtual desktop.
		/// </summary>
		public void Hide()
		{
			_windows.ForEach( w => w.Hide() );
		}
	}
}
