using Laevo.ViewModel.User.Binding;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;


namespace Laevo.ViewModel.User
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	public class UserViewModel : AbstractViewModel
	{
		internal readonly Model.User User;


		[NotifyProperty( Binding.Properties.Name )]
		public string Name { get; set; }


		public UserViewModel( Model.User user )
		{
			User = user;
			Name = user.Name;
		}


		protected override void FreeUnmanagedResources()
		{
			// Nothing to do.
		}

		public override void Persist()
		{
			User.Name = Name;
		}
	}
}
