using System.Runtime.Serialization;


namespace Laevo.Data.Model
{
	[DataContract]
	class Base64Bitmap
	{
		[DataMember]
		public string Base64Image { get; private set; }

		public Base64Bitmap( string base64Image )
		{
			Base64Image = base64Image;
		}
	}
}