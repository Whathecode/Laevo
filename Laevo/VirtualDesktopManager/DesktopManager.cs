using System.Collections.Generic;
using System.Linq;
using Whathecode.System.Windows.Interop;


namespace VirtualDesktopManager
{
	/// <summary>
	///   Allows creating and switching between different <see cref="VirtualDesktop" />'s.
	/// </summary>
	public class DesktopManager
	{
		readonly List<WindowInfo> _ignoreWindows;
		public List<VirtualDesktop> AvailableDesktops = new List<VirtualDesktop>();
		public VirtualDesktop CurrentDesktop { get; private set; }


		public DesktopManager()
		{
			// Determine which windows shouldn't be managed by the desktop manager.
			_ignoreWindows = WindowManager.GetWindows().Where( w => !IsValidWindow( w ) ).ToList();

			CurrentDesktop = new VirtualDesktop( GetOpenWindows() );
			AvailableDesktops.Add( CurrentDesktop );
		}


		/// <summary>
		///   Create an empty virtual desktop with no windows assigned to it.
		/// </summary>
		/// <returns>The newly created virtual desktop.</returns>
		public VirtualDesktop CreateEmptyDesktop()
		{
			var newDesktop = new VirtualDesktop();
			AvailableDesktops.Add( newDesktop );

			return newDesktop;
		}

		/// <summary>
		///   Switch to the given virtual desktop.
		/// </summary>
		/// <param name="desktop">The desktop to switch to.</param>
		public void SwitchToDesktop( VirtualDesktop desktop )
		{
			// Update which windows are associated to the current virtual desktop.
			IEnumerable<WindowInfo> newWindows = GetOpenWindows()
				.Except( AvailableDesktops.SelectMany( d => d.Windows ) )
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
			AvailableDesktops.ForEach( d => d.Show() );
		}

		static bool IsValidWindow( WindowInfo window )
		{
			return window.IsVisible();
		}

		IEnumerable<WindowInfo> GetOpenWindows()
		{
			return WindowManager.GetWindows().Except( _ignoreWindows );
		}
	}
}
