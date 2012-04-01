using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace Laevo.ViewModel.ActivityOverview
{
	public class DataContractSurrogate : IDataContractSurrogate
	{
		[DataContract]
		class SerializedBitmap
		{
			public SerializedBitmap( Uri source )
			{
				Source = source;
			}

			[DataMember]
			public Uri Source { get; private set; }
		}


		public Type GetDataContractType( Type type )
		{
			var convertTypes = new Dictionary<Type, Type>
			{
				{ typeof( ImageSource ), typeof( SerializedBitmap ) },
				{ typeof( SerializedBitmap ), typeof( BitmapImage ) }
			};

			return convertTypes.ContainsKey( type ) ? convertTypes[ type ] : type;
		}

		public object GetObjectToSerialize( object obj, Type targetType )
		{
			if ( targetType == typeof( SerializedBitmap ) )
			{
				return new SerializedBitmap( ((BitmapImage)obj).UriSource );
			}

			return obj;
		}

		public object GetDeserializedObject( object obj, Type targetType )
		{
			SerializedBitmap bitmap = obj as SerializedBitmap;
			if ( bitmap != null )
			{
				return new BitmapImage( bitmap.Source );
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
			// Nothing to do.
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
