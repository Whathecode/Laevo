using Laevo.Model;


namespace Laevo.Logging
{
	class LogData
	{
		public string Key { get; private set; }

		public object Value { get; private set; }

		public object ValueAnonymized { get; private set; }


		/// <summary>
		///   Create a new key/value pair to be stored as part of a log entry.
		/// </summary>
		public LogData( string key, object value, object valueAnonymized = null )
		{
			Key = key;
			Value = value;
			ValueAnonymized = valueAnonymized ?? value;
		}

		/// <summary>
		///   Identify which activity a log entry relates to using "Activity" as the key to store the value.
		/// </summary>
		/// <param name = "activity">The activity to identify.</param>
		public LogData( Activity activity )
			: this( "Activity", activity )
		{
		}

		/// <summary>
		///   Identify which activity a log entry relates to.
		/// </summary>
		/// <param name = "key">A custom key.</param>
		/// <param name = "activity">The activity to identify.</param>
		public LogData( string key, Activity activity )
		{
			Key = key;
			Value = new { activity.Name, activity.Identifier };
			ValueAnonymized = new { Name = "Anonymized", activity.Identifier };
		}
	}
}
