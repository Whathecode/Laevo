
using ABC.Workspaces.Windows;


namespace Laevo.ViewModel.Main.Unresponsive
{
	class UnresponsiveWindow
	{
		public UnresponsiveWindow( string processName, int processId, WindowSnapshot windowSnapshot )
		{
			ProcessName = processName;
			ProcessId = processId;
			WindowSnapshot = windowSnapshot;
		}

		public string ProcessName { get; private set; }
		public int ProcessId { get; private set; }
		public WindowSnapshot WindowSnapshot { get; private set; }
	}
}
