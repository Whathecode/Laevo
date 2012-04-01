using System.Runtime.Serialization;


namespace Laevo.ViewModel
{
	[DataContract]
	abstract class AbstractViewModel
	{
		public abstract void Persist();
	}
}
