using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using MongoDB.Bson;
using Newtonsoft.Json;
using NLog;
using NLog.Targets;
using Segment;
using Segment.Model;


namespace Laevo.Logging
{
	/// <summary>
	///   Logging target used to send logging data as analytics events.
	///   TODO: Make sure only Laevo can upload analytics data.
	/// </summary>
	[Target( "Analytics" )]
	class AnalyticsLogTarget : TargetWithLayout
	{
		static readonly string UserId;

		static readonly string AnalyticsCacheFileName
			= Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Laevo", "AnalyticsCache.xml" );

		static readonly DataContractSerializer AnalyticsSerializer;
		static readonly Queue<AnalyticsData> AnalyticsQueue = new Queue<AnalyticsData>();
		static bool _isClosing;

		static readonly MongoDb Mongo = new MongoDb();

		static AnalyticsLogTarget()
		{
			const string analyticsKey = "32gkxq3ekp";
			Guid userGuid = Properties.Settings.Default.AnalyticsID;
			if ( userGuid == Guid.Empty )
			{
				userGuid = Guid.NewGuid();
				Properties.Settings.Default.AnalyticsID = userGuid;
				Properties.Settings.Default.FirstRun = DateTime.Now;
				Properties.Settings.Default.Save();
			}
			var userTraits = new Traits
			{
				{ "createdAt", Properties.Settings.Default.FirstRun }
			};
#if DEBUG
			Analytics.Initialize( analyticsKey, new Config().SetAsync( true ) );
			userTraits.Add( "name", "Debug" );
			userTraits.Add( "description", "User used during debugging." );
#else
			Analytics.Initialize( analyticsKey );
#endif
			Analytics.Client.Succeeded += ClientSucceeded;
			Analytics.Client.Failed += ClientFailed;
			UserId = Properties.Settings.Default.AnalyticsID.ToString();
			AnalyticsSerializer = new DataContractSerializer( typeof( Queue<AnalyticsData> ), new[] { typeof(List<string>) } );

			if ( File.Exists( AnalyticsCacheFileName ) )
			{
				using ( var analyticsFileStream = new FileStream( AnalyticsCacheFileName, FileMode.Open ) )
				{
					AnalyticsQueue = (Queue<AnalyticsData>)AnalyticsSerializer.ReadObject( analyticsFileStream );
				}
			}

			var identifyProperties = new Segment.Model.Properties();
			foreach ( var ut in userTraits )
			{
				identifyProperties.Add( ut.Key, ut.Value );
			}
			AnalyticsQueue.Enqueue( new AnalyticsData( UserId, "Identify", identifyProperties ) );

			Analytics.Client.Identify( UserId, userTraits );
		}


		static void PersistCache()
		{
			// TODO: Improve data persistance. 
			// In order to persist properties data, values have to be converted to string (some data will be lost). 
			// DataContract serializer cannot serialize list of unknown type.
			Queue<AnalyticsData> tempAnalyticsQueue = AnalyticsQueue;
			foreach ( var analyticsData in tempAnalyticsQueue )
			{
				var tempProperties = new Segment.Model.Properties();
				foreach ( var property in analyticsData.Properties )
				{
					tempProperties.Add( property.Key, property.Value.ToString() );
				}
				analyticsData.Properties = tempProperties;
			}

			using ( var analyticsFileStream = new FileStream( AnalyticsCacheFileName, FileMode.Create ) )
			{
				AnalyticsSerializer.WriteObject( analyticsFileStream, tempAnalyticsQueue );
			}
		}


		protected override void Write( LogEventInfo logEvent )
		{
			var currentData = new AnalyticsData( UserId );

			// Log warnings and errors.
			if ( logEvent.Level > LogLevel.Info )
			{
				currentData.Properties = new Segment.Model.Properties { { "Message", logEvent.FormattedMessage } };
				if ( logEvent.Exception != null )
				{
					currentData.Properties.Add( "Exception", logEvent.Exception.ToString() );
				}
				currentData.EventName = logEvent.Level.ToString();
			}
				// Log info messages.
			else
			{
				// Create event name.
				string loggerName = logEvent.LoggerName;
				currentData.EventName = loggerName.Split( new[] { '.' }, StringSplitOptions.RemoveEmptyEntries ).FirstOrDefault() ?? loggerName;
				currentData.EventName += ": " + logEvent.FormattedMessage;

				// Create properties.
				currentData.Properties = new Segment.Model.Properties();

				foreach ( var p in logEvent.Properties )
				{
					currentData.Properties.Add( p.Key.ToString(), p.Value );
				}
			}

			AnalyticsQueue.Enqueue( currentData );
			Analytics.Client.Track( currentData.UserId, currentData.EventName, currentData.Properties );

			// TODO: Change this dirty hack to detect application shutdown.
			if ( currentData.EventName == "Laevo: Exited." )
			{
				_isClosing = true;
			}
		}

		static void ClientFailed( BaseAction action, Exception e )
		{
			lock ( AnalyticsQueue )
			{
				PersistCache();
			}
		}

		static void ClientSucceeded( BaseAction action )
		{
			lock ( AnalyticsQueue )
			{
				var analyticsData = AnalyticsQueue.FirstOrDefault();
				if ( analyticsData != null )
				{
					analyticsData.IsMongoItem = true;
					PersistCache();
					bool insertResult = Mongo.Insert( BsonDocument.Parse( JsonConvert.SerializeObject( analyticsData ) ) );
					if ( insertResult )
					{
						AnalyticsQueue.Dequeue();
						PersistCache();
					}
				}

				// Reupload cached data.
				var cachedAnalitycsData = AnalyticsQueue.FirstOrDefault();
				if ( cachedAnalitycsData != null )
				{
					// Reupload data only to MongoDB.
					if ( cachedAnalitycsData.IsMongoItem )
					{
						bool insertResult = Mongo.Insert( BsonDocument.Parse( JsonConvert.SerializeObject( cachedAnalitycsData ) ) );
						if ( insertResult )
						{
							AnalyticsQueue.Dequeue();
							PersistCache();
						}
					}
						// Reupload data to analytics segment and MongoDB.
					else
					{
						Analytics.Client.Track( cachedAnalitycsData.UserId, cachedAnalitycsData.EventName, cachedAnalitycsData.Properties,
							new Options().SetTimestamp( cachedAnalitycsData.TimeStamp ) );
					}
				}
			}

			// When Analytics is started in async mode is has to be disposed to avoid infinite thread running in the background.
			if ( _isClosing )
			{
				Analytics.Dispose();
			}
		}
	}
}