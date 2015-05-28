using System;
using System.Runtime.Serialization;
using System.Windows.Media;


namespace Laevo.Model
{
	[DataContract( IsReference = true )]
	public class User
	{
		[DataMember]
		internal readonly Guid Identifier;

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public ImageSource Image { get; set; }

		public User()
		{
			Identifier = Guid.NewGuid();
		}


		public override bool Equals( object obj )
		{
			var user = obj as User;

			if ( user == null )
			{
				return false;
			}

			return Identifier.Equals( user.Identifier );
		}

		public override int GetHashCode()
		{
			return Identifier.GetHashCode();
		}
	}
}
