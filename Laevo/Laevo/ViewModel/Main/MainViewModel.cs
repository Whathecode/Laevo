using System.Windows;
using Laevo.ViewModel.Main.Binding;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.Main
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class MainViewModel
	{
		[CommandExecute( Commands.Exit )]
		public void Exit()
		{
			Application.Current.Shutdown();
		}
	}
}
