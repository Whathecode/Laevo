using System.Collections.ObjectModel;
using Laevo.Model;
using Laevo.Model.AttentionShifts;


namespace Laevo.Data.Model
{
	/// <summary>
	///   Provides access to the persisted model data of Laevo.
	/// </summary>
	/// <author>Steven Jeuris</author>
	public interface IModelRepository
	{
		ReadOnlyCollection<Activity> Activities { get; }
		ReadOnlyCollection<Activity> Tasks { get; }
		ReadOnlyCollection<AbstractAttentionShift> AttentionShifts { get; }

		Activity HomeActivity { get; }
		Settings Settings { get; }

		Activity CreateNewActivity( string name );
		Activity CreateNewTask( string name = "New Task" );
		void CreateActivityFromTask( Activity task );
		void RemoveActivity( Activity activity );
		void SwapTaskOrder( Activity task1, Activity task2 );
		void AddAttentionShift( AbstractAttentionShift attentionShift );

		void SaveChanges();
	}
}
