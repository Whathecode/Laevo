using System.Collections.Generic;
using System.Collections.ObjectModel;
using Laevo.Model;
using Laevo.ViewModel.Activity;


namespace Laevo.Data.View
{
	/// <summary>
	///   Provides access to the persisted view data of Laevo.
	/// </summary>
	/// <author>Steven Jeuris</author>
	public interface IViewRepository
	{
		ActivityViewModel Home { get; set; }
		ObservableCollection<ActivityViewModel> Activities { get; }
		ObservableCollection<ActivityViewModel> Tasks { get; }

		ActivityViewModel LoadActivity( Activity activity );
		void LoadActivities( Activity parentActivity );
		List<ActivityViewModel> GetPath( ActivityViewModel activity );

		void SaveChanges();
	}
}
