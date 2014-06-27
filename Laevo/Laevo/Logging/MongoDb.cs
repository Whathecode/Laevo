using System;
using MongoDB.Bson;
using MongoDB.Driver;


namespace Laevo.Logging
{
	class MongoDb
	{
		const string DatabaseName = "laevo";
		const string CollectionName = "log_data";
		const string ConnectionString = "mongodb://laevoUser:test1234@ds037007.mongolab.com:37007/laevo";
		readonly MongoCollection _logCollection;

		public MongoDb()
		{
			var client = new MongoClient( ConnectionString );
			_logCollection = client.GetServer().GetDatabase( DatabaseName ).GetCollection( CollectionName );
		}

		/// <summary>
		/// Istert data to MongoDB.
		/// </summary>
		/// <returns>True if success, false if fail.</returns>
		public bool Insert( BsonDocument document )
		{
			try
			{
				return _logCollection.Insert( document ).Ok;
			}
			catch ( Exception )
			{
				return false;
			}
		}
	}
}