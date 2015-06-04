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
		protected ObservableCollection<ActivityViewModel> InnerActivities = new ObservableCollection<ActivityViewModel>();
		public ReadOnlyObservableCollection<ActivityViewModel> Activities { get; private set; }
		protected ObservableCollection<ActivityViewModel> InnerTasks = new ObservableCollection<ActivityViewModel>(); 
		public ReadOnlyObservableCollection<ActivityViewModel> Tasks { get; private set; }

		readonly Dictionary<User, UserViewModel> _users = new Dictionary<User, UserViewModel>();


		protected AbstractMemoryViewRepository()
		{
            ServiceLocator.GetInstance().RegisterService<IViewRepository>(this);
			Activities = new ReadOnlyObservableCollection<ActivityViewModel>( InnerActivities );
			Tasks = new ReadOnlyObservableCollection<ActivityViewModel>( InnerTasks );
		}


		public abstract ActivityViewModel LoadActivity( Activity activity );
		public abstract void LoadActivities( Activity parentActivity );
		public abstract void LoadPersonalActivities();
		public abstract List<ActivityViewModel> GetPath( ActivityViewModel activity );
		public abstract void AddActivity( ActivityViewModel activity, ActivityViewModel toParent = null );
		public abstract void RemoveActivity( ActivityViewModel activity );
		public abstract void MoveActivity( ActivityViewModel activity, ActivityViewModel toParent );
		public abstract void UpdateActivity( ActivityViewModel activity );
		public abstract void SwapTaskOrder( ActivityViewModel task1, ActivityViewModel task2 );

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
