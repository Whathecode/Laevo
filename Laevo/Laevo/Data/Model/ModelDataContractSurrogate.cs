using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Laevo.Peer;


namespace Laevo.Data.Model
{
	public class ModelDataContractSurrogate : IDataContractSurrogate
	{
		public Type GetDataContractType( Type type )
		{
			var convertTypes = new Dictionary<Type, Type>
			{
				{ typeof( ImageSource ), typeof( Base64Bitmap ) },
				{ typeof( Base64Bitmap ), typeof( BitmapImage ) }
			};

			return convertTypes.ContainsKey( type ) ? convertTypes[ type ] : type;
		}

		public object GetObjectToSerialize( object obj, Type targetType )
		{
			if ( targetType == typeof( Base64Bitmap ) )
			{
				byte[] data;
				var encoder = new PngBitmapEncoder();

					var bitmapImage = (BitmapSource)obj;
					encoder.Frames.Add( BitmapFrame.Create( bitmapImage ) );

				using ( var ms = new MemoryStream() )
				{
					encoder.Save( ms );
					data = ms.ToArray();
				}
				return new Base64Bitmap( Convert.ToBase64String( data ) );
			}

			return obj;
		}

		public object GetDeserializedObject( object obj, Type targetType )
		{
			if ( targetType == typeof( ImageSource ) )
			{
				var byteBuffer = Convert.FromBase64String( ( (Base64Bitmap)obj ).Base64Image );
				var bitmapImage = new BitmapImage();
				using ( var memoryStream = new MemoryStream( byteBuffer ) )
				{
					bitmapImage.BeginInit();
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.StreamSource = memoryStream;
					bitmapImage.EndInit();
				}

				return bitmapImage;
			}

			return obj;
		}

		public object GetCustomDataToExport( MemberInfo memberInfo, Type dataContractType )
		{
			return null;
		}

		public object GetCustomDataToExport( Type clrType, Type dataContractType )
		{
			return null;
		}

		public void GetKnownCustomDataTypes( Collection<Type> customDataTypes )
		{

		}

		public Type GetReferencedTypeOnImport( string typeName, string typeNamespace, object customData )
		{
			return null;
		}

		public CodeTypeDeclaration ProcessImportedType( CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit )
		{
			return null;
		}
	}
}