using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
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

		// MongoDB
		const string DatabaseName = "laevo";
		const string CollectionName = "log_data";
		const string ConnectionString = "mongodb://laevoUser:test1234@ds037007.mongolab.com:37007/laevo";
		static readonly MongoCollection LogCollection;

		static AnalyticsLogTarget()
		{
			// Initialize analytics for the project stevenjeuris/laevo.
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
			Analytics.Initialize( analyticsKey, new Config().SetAsync( false ) );
			userTraits.Add( "name", "Debug" );
			userTraits.Add( "description", "User used during debugging." );
#else
			Analytics.Initialize( analyticsKey );
#endif
			UserId = Properties.Settings.Default.AnalyticsID.ToString();
			Analytics.Client.Identify( UserId, userTraits );

			var client = new MongoClient( ConnectionString );
			LogCollection = client.GetServer().GetDatabase( DatabaseName ).GetCollection( CollectionName );
		}


		protected override void Write( LogEventInfo logEvent )
		{
			Segment.Model.Properties properties;
			string eventName;

			// Log warnings and errors.
			if ( logEvent.Level > LogLevel.Info )
			{
				properties = new Segment.Model.Properties { { "Message", logEvent.FormattedMessage } };
				if ( logEvent.Exception != null )
				{
					properties.Add( "Exception", logEvent.Exception.ToString() );
				}
				eventName = logEvent.Level.ToString();
			}
				// Log info messages.
			else
			{
				// Create event name.
				string loggerName = logEvent.LoggerName;
				eventName = loggerName.Split( new[] { '.' }, StringSplitOptions.RemoveEmptyEntries ).FirstOrDefault() ?? loggerName;
				eventName += ": " + logEvent.FormattedMessage;

				// Create properties.
				properties = new Segment.Model.Properties();
				foreach ( var p in logEvent.Properties )
				{
					properties.Add( p.Key.ToString(), p.Value );
				}
			}

			Analytics.Client.Track( UserId, eventName, properties );
			SaveToMongo( UserId, eventName, properties );
		}

		void SaveToMongo( string userId, string eventName, Segment.Model.Properties properties )
		{
			// Add main event data.
			var eventData = new BsonDocument
			{
				{ "UserId", userId },
				{ "EventName", eventName },
			};

			// Add all properties data to nested node of main.
			if ( properties.Count > 0 )
			{
				var propertiesData = new BsonDocument();
				propertiesData.AddRange( properties );
				eventData.Add( new BsonElement( "Properties", propertiesData ) );
			}

			// Automaticaly connect to dababase and insert data.
			LogCollection.Insert( eventData );
		}
	}
}