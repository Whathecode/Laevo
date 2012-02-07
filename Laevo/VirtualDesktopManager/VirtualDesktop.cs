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
	public class VirtualDesktop
	{
		List<WindowInfo> _windows = new List<WindowInfo>();
		internal ReadOnlyCollection<WindowInfo> Windows
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
		/// <param name="newWindows">Newly opened windows on this virtual desktop.</param>
		public void UpdateWindows( IEnumerable<WindowInfo> newWindows )
		{
			// Add new windows.
			_windows.AddRange( newWindows );

			// Remove windows which are no longer open.
			_windows = _windows.Where( w => !w.IsDestroyed() ).ToList();
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
