using System.Collections.Generic;
using Whathecode.System.Windows.Interop;
using System.Diagnostics;


namespace VirtualDesktopManager
{
	/// <summary>
	///   Allows creating and switching between different <see cref="VirtualDesktop" />'s.
	/// </summary>
	public class DesktopManager
	{
		public VirtualDesktop CurrentDesktop { get; private set; }


		public DesktopManager()
		{
			CurrentDesktop = new VirtualDesktop( WindowManager.GetWindows() );
		}


		public VirtualDesktop CreateEmptyDesktop()
		{
			return new VirtualDesktop();
		}

		/// <summary>
		///   Switch to the given virtual desktop.
		/// </summary>
		/// <param name="desktop">The desktop to switch to.</param>
		public void SwitchToDesktop( VirtualDesktop desktop )
		{
			IEnumerable<WindowInfo> oldWindows = CurrentDesktop.Windows;
			List<WindowInfo> currentWindows = WindowManager.GetWindows();

			foreach ( var window in currentWindows )
			{
				Debug.WriteLine( window.GetTitle() + ": " + window.GetWindowState() );
			}

			CurrentDesktop = desktop;
		}
	}
}
