using System.Collections.ObjectModel;
using Laevo.ViewModel.Activity;


namespace Laevo.Data.View
{
	/// <summary>
	///   An abstract class which provides view data for Laevo which is held in memory.
	/// </summary>
	/// <author>Steven Jeuris</author>
	abstract class AbstractMemoryViewRepository : IViewRepository
	{
		readonly ObservableCollection<ActivityViewModel> _activities = new ObservableCollection<ActivityViewModel>();
		public ObservableCollection<ActivityViewModel> Activities
		{
			get { return _activities; }
		}

		readonly ObservableCollection<ActivityViewModel> _tasks = new ObservableCollection<ActivityViewModel>(); 
		public ObservableCollection<ActivityViewModel> Tasks
		{
			get { return _tasks; }
		}


		public abstract void SaveChanges();
	}
}
