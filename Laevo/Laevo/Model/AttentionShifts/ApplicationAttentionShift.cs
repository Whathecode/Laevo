using System.Runtime.Serialization;


namespace Laevo.Model.AttentionShifts
{
	/// <summary>
	///   Represents a shift of attention towards a certain part of the application.
	/// </summary>
	/// <author>Steven Jeuris</author>
	[DataContract]
	class ApplicationAttentionShift : AbstractAttentionShift
	{
		public enum Application
		{
			Startup,
			Shutdown
		}


		[DataMember]
		public Application ApplicationPart { get; private set; }


		public ApplicationAttentionShift( Application applicationPart )
		{
			ApplicationPart = applicationPart;
		}
	}
}
