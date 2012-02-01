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
		internal ReadOnlyCollection<WindowInfo> Windows { get; private set; }


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
			Windows = initialWindows.ToList().AsReadOnly();
		}
	}
}
