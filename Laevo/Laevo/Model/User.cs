using System;
using System.Runtime.Serialization;


namespace Laevo.Model
{
	[DataContract]
	public class User
	{
		[DataMember]
		Guid _identifier;

		[DataMember]
		public string Name { get; set; }


		public User()
		{
			_identifier = Guid.NewGuid();
		}
	}
}
