using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Laevo.Model;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.User;


namespace Laevo.Data.View
{
	/// <summary>
	///   An abstract class which provides view data for Laevo which is held in memory.
	/// </summary>
	/// <author>Steven Jeuris</author>
	abstract class AbstractMemoryViewRepository : IViewRepository
	{
		public UserViewModel User { get; protected set; }
		public ActivityViewModel Home { get; set; }
		public ObservableCollection<ActivityViewModel> Activities { get; private set; }
		public ObservableCollection<ActivityViewModel> Tasks { get; private set; }

		readonly Dictionary<User, UserViewModel> _users = new Dictionary<User, UserViewModel>();


		protected AbstractMemoryViewRepository()
		{
			Activities = new ObservableCollection<ActivityViewModel>();
			Tasks = new ObservableCollection<ActivityViewModel>();
		}


		public abstract ActivityViewModel LoadActivity( Activity activity );
		public abstract void LoadActivities( Activity parentActivity );
		public abstract List<ActivityViewModel> GetPath( ActivityViewModel activity );
		
		public abstract void AddActivity( ActivityViewModel activity, Guid parent );
		public abstract void RemoveActivity( ActivityViewModel activity );
		public UserViewModel GetUser( User user )
		{
			UserViewModel viewModel;
			if ( !_users.TryGetValue( user, out viewModel ) )
			{
				viewModel = new UserViewModel( user );
				_users[ user ] = viewModel;
			}

			return viewModel;
		}

		public abstract void SaveChanges();
	}
}
