using System.Runtime.Serialization;
using ABC.Applications;


namespace ABC.Interruptions.Google
{
	[DataContract]
	public class GmailInterruption : AbstractInterruption
	{
		[DataMember]
		readonly string _link;


		public GmailInterruption( string name, string link )
			: base( name )
		{
			_link = link;
		}


		protected override void OnInterruptionOpened()
		{
			ServiceProvider.Browser.OpenWebsite( _link );
		}
	}
}
