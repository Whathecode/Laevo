using System.Collections.Generic;
using System.Collections.ObjectModel;
using Laevo.Model;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.User;


namespace Laevo.Data.View
{
	/// <summary>
	///   Provides access to the persisted view data of Laevo.
	/// </summary>
	/// <author>Steven Jeuris</author>
	public interface IViewRepository
	{
		UserViewModel User { get; }
		ActivityViewModel Home { get; set; }
		ObservableCollection<ActivityViewModel> Activities { get; }
		ObservableCollection<ActivityViewModel> Tasks { get; }

		ActivityViewModel LoadActivity( Activity activity );
		void LoadActivities( Activity parentActivity );
		List<ActivityViewModel> GetPath( ActivityViewModel activity );

		UserViewModel GetUser( User user );

		void SaveChanges();
	}
}
