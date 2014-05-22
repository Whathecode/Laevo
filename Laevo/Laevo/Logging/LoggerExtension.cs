using System.Collections.Generic;
using NLog;


namespace Laevo.Logging
{
	static class LoggerExtension
	{
		/// <summary>
		///   Writes the diagnostic message at the <see cref="LogLevel.Info">Info</see> level with additional attached properties.
		/// </summary>
		/// <param name = "logger">The logger which will log this event.</param>
		/// <param name = "message">The log message.</param>
		/// <param name = "properties">Extra key/value properties which will be attached to the log event.</param>
		static public void InfoWithData( this Logger logger, string message, params LogData[] properties )
		{
			LogWithData( logger, LogLevel.Info, message, properties );
		}

		/// <summary>
		///   Writes the diagnostic message at the <see cref="LogLevel.Debug">Debug</see> level with additional attached properties.
		/// </summary>
		/// <param name = "logger">The logger which will log this event.</param>
		/// <param name = "message">The log message.</param>
		/// <param name = "properties">Extra key/value properties which will be attached to the log event.</param>
		static public void DebugWithData( this Logger logger, string message, params LogData[] properties )
		{
			LogWithData( logger, LogLevel.Debug, message, properties );
		}

		static void LogWithData( this Logger logger, LogLevel logLevel, string message, IEnumerable<LogData> properties )
		{
			var log = new LogEventInfo( logLevel, logger.Name, message );
			foreach ( var p in properties )
			{
				log.Properties.Add( p.Key, p.Value );
			}

			logger.Log( typeof( LoggerExtension ), log );
		}
	}
}
