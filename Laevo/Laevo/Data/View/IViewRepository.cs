using System.Collections.ObjectModel;
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

		void SaveChanges();
	}
}
