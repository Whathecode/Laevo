using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;


namespace ABC.Interruptions.Survey
{
	/// <summary>
	///   Receives survey notifications and introduces them as interruptions.
	/// </summary>
	[Export( typeof( AbstractInterruptionTrigger ) )]
	public class SurveyInterupptionTrigger : AbstractIntervalInterruptionTrigger
	{
		const string SurveyAtomFeed = "http://members.upcpoczta.pl/z.grondziowska/SurveyFeed3.xml";

		readonly List<string> _processedSurveyIds = new List<string>();
		readonly string _surveysFile;
		
		// Serializes survey ids which was previously shown to the user.
		readonly DataContractSerializer _surveySerializer;
		
		readonly string _asseblyPath = Assembly.GetExecutingAssembly().Location;

		public SurveyInterupptionTrigger()
			: base( TimeSpan.FromMinutes( 1 ) )
		{
			_asseblyPath = _asseblyPath.Remove( _asseblyPath.LastIndexOf( '\\' ) );
			_surveysFile = Path.Combine( _asseblyPath, "ProcessedSurveys.xml" );
			_surveySerializer = new DataContractSerializer( typeof( List<string> ) );

			if ( File.Exists( _surveysFile ) )
			 {
				List<string> processedList;
				using ( var fileStream = new FileStream( _surveysFile, FileMode.Open ) )
				{
					processedList = (List<string>)_surveySerializer.ReadObject( fileStream );
				}
				_processedSurveyIds.AddRange( processedList );
			}
		}

		static bool HasInternetConnection()
		{
			try
			{
				using ( var client = new WebClient() )
				using ( client.OpenRead( "http://www.google.com" ) )
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

		protected override void IntervalUpdate( DateTime now )
		{
			if ( !HasInternetConnection() )
			{
				return;
			}

			// Retrieving the survey stream.
			Stream newsStream = null;
			while ( newsStream == null )
			{
				var client = new WebClient();
				newsStream = client.OpenRead( SurveyAtomFeed );
			}

			// Open the atom feed.
			var doc = new XmlDocument();
			doc.Load( newsStream );
			var namespaceManager = new XmlNamespaceManager( doc.NameTable );
			namespaceManager.AddNamespace( "atom", "http://www.w3.org/2005/Atom" );

			// Trigger an interruption for every new survey.
			var idsInFeed = new List<string>();
			XmlNodeList entries = doc.SelectNodes( "//atom:entry", namespaceManager );

			if ( entries == null )
			{
				return;
			}

			foreach ( XmlNode entry in entries )
			{
				XmlNode titleNode = entry[ "title" ];
				if ( titleNode == null )
				{
					break;
				}
				string title = titleNode.InnerText;

				XmlNode linkNode = entry[ "link" ];
				if ( linkNode == null || linkNode.Attributes == null )
				{
					break;
				}
				string link = linkNode.Attributes[ "href" ].InnerText;

				XmlNode idNode = entry[ "id" ];
				if ( idNode == null )
				{
					break;
				}
				string id = idNode.InnerText;

				XmlNode targetUserIdNode = entry[ "targetUserId" ];
				if ( targetUserIdNode == null )
				{
					break;
				}
				string targetUserId = targetUserIdNode.InnerText;

				idsInFeed.Add( id );
				if ( !_processedSurveyIds.Contains( id ) )
				{
					TriggerInterruption( new SurveyInterruption( title, link, targetUserId ) );
					_processedSurveyIds.Add( id );
				}
			}

			// Remove all surveys ids that were stored as shown but are not present in atom feed.
			var toRemove = _processedSurveyIds.Where( s => !idsInFeed.Contains( s ) ).ToList();
			toRemove.ForEach( r => _processedSurveyIds.Remove( r ) );
			using ( var fileStream = new FileStream( _surveysFile, FileMode.Create ) )
			{
				_surveySerializer.WriteObject( fileStream, _processedSurveyIds );
			}
		}

		public override List<Type> GetInterruptionTypes()
		{
			return new List<Type> { typeof( SurveyInterruption ) };
		}
	}
}