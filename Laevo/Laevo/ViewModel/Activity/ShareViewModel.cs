using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Laevo.Data.View;
using Laevo.Peer;
using Laevo.ViewModel.Activity.Binding;
using Laevo.ViewModel.User;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.Activity
{
	[ViewModel( typeof( ShareProperties ), typeof( ShareCommands ) )]
	public class ShareViewModel
	{
		readonly IUsersPeer _usersPeer;
		readonly IViewRepository _repository;

		[NotifyProperty( ShareProperties.Activity )]
		public ActivityViewModel Activity { get; private set; }

		public ObservableCollection<UserViewModel> RetrievedUsers { get; private set; }


		public ShareViewModel( Model.Laevo model, ActivityViewModel activity, IViewRepository repository )
		{
			_usersPeer = model.UsersPeer;
			_repository = repository;
			Activity = activity;
			RetrievedUsers = new ObservableCollection<UserViewModel>();
		}


		[CommandExecute( ShareCommands.SearchUsers )]
		public async void SearchUsers( string searchTerm )
		{
			RetrievedUsers.Clear();

			List<Model.User> users = await _usersPeer.GetUsers( searchTerm );
			users.Select( u => _repository.GetUser( u ) ).ForEach( RetrievedUsers.Add );
		}

		[CommandExecute( ShareCommands.InviteUser )]
		public void InviteUser( UserViewModel user )
		{
		    //_usersPeer.Invite( user.User, Activity.Activity );
		    Activity.InviteUser( user );
		}
	}
}
