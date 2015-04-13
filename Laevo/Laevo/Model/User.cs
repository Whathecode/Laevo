using System;
using System.Runtime.Serialization;


namespace Laevo.Model
{
	[DataContract]
	public class User
	{
		[DataMember]
		Guid _identifier;


		public User()
		{
			_identifier = Guid.NewGuid();
		}
	}
}
