﻿using System.Windows;
using System.Windows.Input;


namespace Laevo.View.Common
{
	static class Actions
	{
		/// <summary>
		///   For the source of an UIelement to be updated by moving the focus.
		/// </summary>
		public static void ForceUpdate( UIElement element )
		{
			// Moving focus also updates the source.
			element.MoveFocus( new TraversalRequest( FocusNavigationDirection.Previous ) );

			// TODO: For text boxes the source can be updated as follows, but this doesn't move the caret.
			/*var nameBinding = ActivityName.GetBindingExpression( TextBox.TextProperty );
			if ( nameBinding != null && !ActivityName.IsReadOnly && ActivityName.IsEnabled )
			{
				nameBinding.UpdateSource();
			}*/
		}
	}
}
