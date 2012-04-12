using System.Runtime.Serialization;


namespace Laevo.Model.AttentionShifts
{
	/// <summary>
	///   Represents a shift of attention towards a certain activity.
	/// </summary>
	[DataContract]
	class ActivityAttentionShift : AbstractAttentionShift
	{
		/// <summary>
		///   The activity towards which attention shifted.
		/// </summary>
		[DataMember]
		public Activity Activity { get; private set; }


		public ActivityAttentionShift( Activity activity )
		{
			Activity = activity;
		}
	}
}
