using System.Windows;
using System.Windows.Input;


namespace Laevo.View
{
	static class Common
	{
		/// <summary>
		///   For the source of an UIelement to be updated by moving the focus.
		/// </summary>
		public static void ForceUpdate( UIElement element )
		{
			// Moving focus also updates the source.
			element.MoveFocus( new TraversalRequest( FocusNavigationDirection.Previous ) );
		}
	}
}
