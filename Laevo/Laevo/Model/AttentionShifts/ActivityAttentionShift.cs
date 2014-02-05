using System.Runtime.Serialization;


namespace Laevo.Model.AttentionShifts
{
	/// <summary>
	///   Represents a shift of attention towards a certain activity.
	/// </summary>
	[DataContract]
	public class ActivityAttentionShift : AbstractAttentionShift
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


		public void ActivityRemoved()
		{
			Activity = null;
		}
	}
}
