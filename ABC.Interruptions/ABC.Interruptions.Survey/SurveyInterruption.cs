using System.Runtime.Serialization;

namespace ABC.Interruptions.Survey
{
	[DataContract]
	internal class SurveyInterruption : AbstractInterruption
	{
		[DataMember]
		readonly string _link;

		/// <summary>
		/// Id of user who interruption is addessed to, all is default value- every user will be notified.
		/// </summary>
		[DataMember]
		public string TargetUserId { get; private set; }


		public SurveyInterruption(string name, string link, string targetUserId)
			: base(name)
		{
			_link = link;
			TargetUserId = targetUserId;
		}


		protected override void OnInterruptionOpened()
		{
			ServiceProvider.Browser.OpenWebsite(_link);
		}
	}
}