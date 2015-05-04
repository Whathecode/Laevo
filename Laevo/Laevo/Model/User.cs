using System;
using System.Runtime.Serialization;
using System.Windows.Media;


namespace Laevo.Model
{
	[DataContract( IsReference = true )]
	public class User
	{
		[DataMember]
		readonly Guid _identifier;

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public ImageSource Image { get; set; }

		public User()
		{
			_identifier = Guid.NewGuid();
		}


		public override bool Equals( object obj )
		{
			var user = obj as User;

			if ( user == null )
			{
				return false;
			}

			return _identifier.Equals( user._identifier );
		}

		public override int GetHashCode()
		{
			return _identifier.GetHashCode();
		}
	}
}
