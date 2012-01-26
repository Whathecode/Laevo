using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Laevo.View.TrayIcon;


namespace Laevo
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		private TrayIconControl _trayIcon;


		protected override void OnStartup( StartupEventArgs e )
		{
			base.OnStartup( e );

			// TODO: Create Model.

			// TODO: Create ViewModel.

			// Create View.
			_trayIcon = new TrayIconControl();
		}
	}
}
