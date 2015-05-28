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
		[DataContract]
		class UsersPeerPlaceholder {}

		[DataContract]
		class RepositoryPlaceholder {}


		readonly IModelRepository _repository;
		readonly IUsersPeer _usersPeer;


		public ModelDataContractSurrogate( IModelRepository repository, IUsersPeer usersPeer )
		{
			_repository = repository;
			_usersPeer = usersPeer;
		}


		public Type GetDataContractType( Type type )
		{
			var convertTypes = new Dictionary<Type, Type>
			{
				{ typeof( UsersPeerPlaceholder ), typeof( IUsersPeer ) },
				{ typeof( RepositoryPlaceholder ), typeof( IModelRepository ) },
				{ typeof( ImageSource ), typeof( Base64Bitmap ) },
				{ typeof( Base64Bitmap ), typeof( BitmapImage ) }
			};

			return convertTypes.ContainsKey( type ) ? convertTypes[ type ] : type;
		}

		public object GetObjectToSerialize( object obj, Type targetType )
		{
			if ( targetType == typeof( IUsersPeer ) )
			{
				return new UsersPeerPlaceholder();
			}
			if ( targetType == typeof( IModelRepository ) )
			{
				return new RepositoryPlaceholder();
			}
			if ( targetType == typeof( Base64Bitmap ) )
			{
				byte[] data;
				var encoder = new PngBitmapEncoder();
				var bitmapImage = (BitmapSource)obj;
				
				// Is some cases BitmapFrame.Create throws "NotSupportedException" which seems to be a WPF problem.
				// To avoid it I call this function with 3 null parameters. More details on: http://stackoverflow.com/a/20990179
				// TODO: More tests are needed to verify if this issue is solved.
				encoder.Frames.Add( BitmapFrame.Create( bitmapImage, null, null, null ) );

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
			if ( targetType == typeof( IUsersPeer ) )
			{
				return _usersPeer;
			}
			if ( targetType == typeof( IModelRepository ) )
			{
				return _repository;
			}
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
			customDataTypes.Add( typeof( UsersPeerPlaceholder ) );
			customDataTypes.Add( typeof( RepositoryPlaceholder ) );
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