using System;
using System.Runtime.Serialization;


namespace Laevo.Logging
{
	[DataContract]
	class AnalyticsData
	{
		[DataMember]
		public string UserId { get; set; }

		[DataMember]
		public string EventName { get; set; }
		
		[DataMember]
		public DateTime TimeStamp { get; set; }
		
		[DataMember]
		public Segment.Model.Properties Properties { get; set; }
		
		[DataMember]
		public bool IsMongoItem { get; set; }


		public AnalyticsData( string userId )
		{
			UserId = userId;
			TimeStamp = DateTime.Now;
		}

		public AnalyticsData( string userId, string eventName, Segment.Model.Properties properties )
			: this( userId )
		{
			EventName = eventName;
			Properties = properties;
		}
	}
}