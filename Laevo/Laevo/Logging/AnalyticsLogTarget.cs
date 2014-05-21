using System;
using System.Linq;
using NLog;
using NLog.Targets;
using Segment;
using Segment.Model;


namespace Laevo
{
	/// <summary>
	///   Logging target used to send logging data as analytics events.
	///   TODO: Make sure only Laevo can upload analytics data.
	/// </summary>
	[Target( "Analytics" )]
	class AnalyticsLogTarget : TargetWithLayout
	{
		static readonly string UserId = Properties.Settings.Default.AnalyticsID.ToString();


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
			string userId = userGuid.ToString();
			Analytics.Client.Identify( userId, userTraits );
		}


		protected override void Write( LogEventInfo logEvent )
		{
			// Log warnings and errors.
			if ( logEvent.Level > LogLevel.Info )
			{
				var properties = new Segment.Model.Properties { { "Message", logEvent.FormattedMessage } };
				if ( logEvent.Exception != null )
				{
					properties.Add( "Exception", logEvent.Exception.ToString() );
				}
				Analytics.Client.Track( UserId, logEvent.Level.ToString(), properties );
			}
			// Log info messages.
			else
			{
				// Create event name.
				string loggerName = logEvent.LoggerName;
				string eventName = loggerName.Split( new[] { '.' }, StringSplitOptions.RemoveEmptyEntries ).FirstOrDefault() ?? loggerName;
				eventName += ": " + logEvent.FormattedMessage;

				// Create properties.
				var properties = new Segment.Model.Properties();
				foreach ( var p in logEvent.Properties )
				{
					properties.Add( p.Key.ToString(), p.Value );
				}

				Analytics.Client.Track( UserId, eventName, properties );
			}
		}
	}
}
