using System.Windows;
using System.Windows.Input;


namespace Laevo.View
{
	static class Common
	{
		/// <summary>
		///   For the source from an originating event to be updated by moving the focus.
		/// </summary>
		/// <param name="e"></param>
		public static void ForceUpdateSource( RoutedEventArgs e )
		{
			var element = (UIElement)e.Source;

			// Moving focus also updates the source.
			element.MoveFocus( new TraversalRequest( FocusNavigationDirection.Previous ) );
		}
	}
}
