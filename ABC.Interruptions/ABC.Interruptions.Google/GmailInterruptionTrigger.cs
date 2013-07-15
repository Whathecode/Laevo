using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Threading;
using System.Xml;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Threading;


namespace ABC.Interruptions.Google
{
	/// <summary>
	///   Receives unread emails from the currently logged in gmail account and introduces them as interruptions.
	/// </summary>
	[Export( typeof( AbstractInterruptionTrigger ) )]
	public class GmailInterruptionTrigger : AbstractIntervalInterruptionTrigger
	{
		const string GmailAtomFeed = "https://mail.google.com/mail/feed/atom";

		readonly Dispatcher _dispatcher;

		readonly Configuration _config;
		readonly GoogleConfiguration _settings;
		const string GmailSection = "GmailSettings";
		static readonly byte[] Entropy = Encoding.Unicode.GetBytes( "Gmail user settings should be saved securely!" );
		SecureString _password;


		public GmailInterruptionTrigger()
			: base( TimeSpan.FromMinutes( 1 ) )
		{
			_dispatcher = Dispatcher.CurrentDispatcher;

			// Recover settings, or ask for them if not asked before.
			_config = ConfigurationManager.OpenExeConfiguration( Assembly.GetExecutingAssembly().Location );
			_settings = _config.Sections.Get( GmailSection ) as GoogleConfiguration;
			if ( _settings != null )
			{
				if ( _settings.Password.Length == 0 )
				{
					return;
				}

				byte[] decryptedData = ProtectedData.Unprotect(
					Convert.FromBase64String( _settings.Password ),
					Entropy,
					DataProtectionScope.CurrentUser );
				_password = Encoding.Unicode.GetString( decryptedData ).ToSecureString();
			}
			else
			{
				_settings = new GoogleConfiguration();
				_config.Sections.Add( GmailSection, _settings );
				AskSettings();
			}
		}


		void AskSettings()
		{
			var askForCredentials = new CredentialsDialog();
			bool? result = askForCredentials.ShowDialog();
			if ( result != null && result.Value )
			{
				_settings.IsEnabled = true;
				_settings.Username = askForCredentials.Username.Text;
				_password = askForCredentials.Password.SecurePassword;
				byte[] encryptedPassword = ProtectedData.Protect(
					Encoding.Unicode.GetBytes( _password.ToInsecureString() ),
					Entropy,
					DataProtectionScope.CurrentUser );
				_settings.Password = Convert.ToBase64String( encryptedPassword );
			}
			else
			{
				_settings.IsEnabled = false;
			}

			_config.Save();
		}

		static bool HasInternetConnection()
		{
			try
			{
				using ( var client = new WebClient() )
				using ( var stream = client.OpenRead( "http://www.google.com" ) )
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

			// Try retrieving the unread email stream until the correct login is specified, or decided not to log in.
			Stream mailStream = null;
			while ( _settings.IsEnabled && mailStream == null )
			{
				var client = new WebClient { Credentials = new NetworkCredential( _settings.Username, _password ) };
				try
				{
					mailStream = client.OpenRead( GmailAtomFeed );
				}
				catch ( WebException )
				{
					// TODO: What when no internet is available?
					DispatcherHelper.SafeDispatch( _dispatcher, AskSettings );
				}
			}

			// Early out when the user does not want to receive email interruptions.
			if ( !_settings.IsEnabled || mailStream == null )
			{
				return;
			}

			// Open op the atom feed.
			var doc = new XmlDocument();
			doc.Load( mailStream );
			var namespaceManager = new XmlNamespaceManager( doc.NameTable );
			namespaceManager.AddNamespace( "atom", "http://purl.org/atom/ns#" );

			// Check whether there are any new unread emails.
			XmlNode modifiedNode = doc.SelectSingleNode( "//atom:modified", namespaceManager );
			if ( modifiedNode == null )
			{
				return;
			}
			DateTime modified = ParseDate( modifiedNode.InnerText );
			if ( _settings.LastModified != null && _settings.LastModified >= modified )
			{
				return;
			}
			_settings.LastModified = modified;
			_config.Save();

			// Trigger an interruption for every unread email.
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
					return;
				}
				string title = titleNode.InnerText;

				XmlNode linkNode = entry[ "link" ];
				if ( linkNode == null || linkNode.Attributes == null )
				{
					return;
				}
				string link = linkNode.Attributes[ "href" ].InnerText;

				TriggerInterruption( new GmailInterruption( ServiceProvider, title, link ) );
			}
		}

		/// <summary>
		///   Apparently the Gmail atom feed sometimes returns incorrect timestamps, after 24:00?
		///   Added this fix as a precaution: http://stackoverflow.com/q/2000343/590790
		/// </summary>
		static DateTime ParseDate( string dateTime )
		{
			DateTime result;
			if ( !DateTime.TryParse( dateTime, out result ) )
			{
				result = DateTime.ParseExact( dateTime, "yyyy-MM-ddT24:mm:ssK", CultureInfo.InvariantCulture );
				result = result.AddDays( 1 );
			}

			return result;
		}

		public override List<Type> GetInterruptionTypes()
		{
			return new List<Type> { typeof( GmailInterruption ) };
		}
	}
}
