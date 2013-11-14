using System;
using System.Runtime.Serialization;


namespace Laevo.Data
{
	/// <summary>
	///   Exception which is thrown when persisting data fails.
	/// </summary>
	[Serializable]
	class PersistenceException : Exception
	{
		public PersistenceException( string message, Exception innerException )
			: base( message, innerException )
		{
		}

		protected PersistenceException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
		}
	}
}
