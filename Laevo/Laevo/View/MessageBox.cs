using System.Windows;
using WindowsMsgBox = System.Windows.MessageBox;


namespace Laevo.View
{
	/// <summary>
	///   Static class used to display message boxes from anywhere within an application.
	///   This class needs to be used, rather than <see cref="System.Windows.MessageBox" /> directly, to ensure they are not closed automatically.
	///   More info: http://stackoverflow.com/questions/22403098/wpf-messagebox-not-waiting-for-result-wpf-notifyicon
	/// </summary>
	public static class MessageBox
	{
		/// <summary>
		///   HACK: This parent window needs to be created to ensure a parent is always available, otherwise the messagebox is closed automatically.
		///   More info: http://stackoverflow.com/questions/22403098/wpf-messagebox-not-waiting-for-result-wpf-notifyicon
		/// </summary>
		static readonly Window Parent;


		static MessageBox()
		{
			Parent = new Window
			{
				Visibility = Visibility.Hidden,
				// Just hiding the window is not sufficient, as it still temporarily pops up the first time. Therefore, make it transparent.
				AllowsTransparency = true,
				Background = System.Windows.Media.Brushes.Transparent,
				WindowStyle = WindowStyle.None,
				ShowInTaskbar = false
			};
			Parent.Show();
		}


		/// <summary>
		///   Displays a message box that has a message and title bar caption; and that returns a result.
		/// </summary>
		/// <param name="messageBoxText">A <see cref="string" /> that specifies the text to display.</param>
		/// <param name="caption">A <see cref="string" />String that specifies the title bar caption to display.</param>
		/// <param name="button">A <see cref="MessageBoxButton" /> value that specifies which button or buttons to display.</param>
		public static void Show( string messageBoxText, string caption, MessageBoxButton button )
		{
			Show( messageBoxText, caption, button, MessageBoxImage.None );
		}

		/// <summary>
		///   Displays a message box that has a message, title bar caption, button, and icon; and that returns a result.
		/// </summary>
		/// <param name="messageBoxText">A <see cref="string" /> that specifies the text to display.</param>
		/// <param name="caption">A <see cref="string" />String that specifies the title bar caption to display.</param>
		/// <param name="button">A <see cref="MessageBoxButton" /> value that specifies which button or buttons to display.</param>
		/// <param name="icon">A <see cref="MessageBoxImage" /> value that specifies the icon to display.</param>
		/// <returns>A <see cref="MessageBoxResult" /> value that specifies which message box button is clicked by the user.</returns>
		public static void Show( string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon )
		{
			WindowsMsgBox.Show( Parent, messageBoxText, caption, button, icon );
		}
	}
}
