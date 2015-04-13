using Laevo.ViewModel.User.Binding;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;


namespace Laevo.ViewModel.User
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	public class UserViewModel : AbstractViewModel
	{
		readonly Model.User _user;


		[NotifyProperty( Binding.Properties.Name )]
		public string Name { get; set; }


		public UserViewModel( Model.User user )
		{
			_user = user;
			Name = user.Name;
		}


		protected override void FreeUnmanagedResources()
		{
			// Nothing to do.
		}

		public override void Persist()
		{
			_user.Name = Name;
		}
	}
}
