using System.Collections.ObjectModel;
using Laevo.ViewModel.Activity;


namespace Laevo.Data.View
{
	/// <summary>
	///   Provides access to the persisted view data of Laevo.
	/// </summary>
	/// <author>Steven Jeuris</author>
	interface IViewRepository
	{
		ObservableCollection<ActivityViewModel> Activities { get; }
		ObservableCollection<ActivityViewModel> Tasks { get; }

		void SaveChanges();
	}
}
