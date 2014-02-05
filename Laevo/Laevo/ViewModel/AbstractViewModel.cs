using System.Runtime.Serialization;
using Whathecode.System;


namespace Laevo.ViewModel
{
	[DataContract]
	public abstract class AbstractViewModel : AbstractDisposable
	{
		public abstract void Persist();

		protected override void FreeManagedResources()
		{
			// Generally nothing to do.
		}
	}
}