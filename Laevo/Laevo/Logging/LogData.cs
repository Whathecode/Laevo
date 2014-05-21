namespace Laevo.Logging
{
	class LogData
	{
		public string Key { get; private set; }

		public object Value { get; private set; }


		public LogData( string key, object value )
		{
			Key = key;
			Value = value;
		}
	}
}
