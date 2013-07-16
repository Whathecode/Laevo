using System.Collections.Specialized;
using System.Configuration;


namespace ABC.Interruptions.Google
{
	public class GoogleConfiguration : ConfigurationSection
	{
		public GoogleConfiguration()
		{
			SectionInformation.AllowExeDefinition = ConfigurationAllowExeDefinition.MachineToRoamingUser;
			ProcessedEmails = new ProcessedEmailCollection();
		}


		[ConfigurationProperty( "isEnabled", DefaultValue = "true" )]
		public bool IsEnabled
		{
			get { return (bool)this[ "isEnabled" ]; }
			set { this[ "isEnabled" ] = value; }
		}

		[ConfigurationProperty( "username" )]
		public string Username
		{
			get { return (string)this[ "username" ]; }
			set { this[ "username" ] = value; }
		}

		[ConfigurationProperty( "password" )]
		public string Password
		{
			get { return (string)this[ "password" ]; }
			set { this[ "password" ] = value; }
		}

		[ConfigurationProperty( "processedEmails" )]
		public ProcessedEmailCollection ProcessedEmails
		{
			get { return (ProcessedEmailCollection)this[ "processedEmails" ]; }
			set { this[ "processedEmails" ] = value; }
		}
	}

	public class ProcessedEmail : ConfigurationElement
	{
		[ConfigurationProperty( "id" )]
		public string Id
		{
			get { return (string)this[ "id" ]; }
			set { this[ "id" ] = value; }
		}
	}

	[ConfigurationCollection( typeof( ConfigurationElement ) )]
	public class ProcessedEmailCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new ProcessedEmail();
		}

		protected override object GetElementKey( ConfigurationElement element )
		{
			return ((ProcessedEmail)(element)).Id;
		}

		public void Add( string id )
		{
			LockItem = false;
			BaseAdd( new ProcessedEmail { Id = id } );
		}

		public void Remove( string id )
		{
			LockItem = false;
			BaseRemove( id );
		}
	}
}