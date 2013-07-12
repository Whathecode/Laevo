using System;
using System.Configuration;


namespace ABC.Interruptions.Google
{
	public class GoogleConfiguration : ConfigurationSection
	{
		public GoogleConfiguration()
		{
			SectionInformation.AllowExeDefinition = ConfigurationAllowExeDefinition.MachineToRoamingUser;
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

		[ConfigurationProperty( "lastIssued" )]
		public DateTime? LastIssued
		{
			get { return (DateTime?)this[ "lastIssued" ]; }
			set { this[ "lastIssued" ] = value; }
		}
	}
}