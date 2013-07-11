using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ABC.Windows;
using ABC.Windows.Desktop;


namespace Laevo.ViewModel.ActivityOverview
{
	public class ActivityDataContractSurrogate : IDataContractSurrogate
	{
		[DataContract]
		class SerializedBitmap
		{
			[DataMember]
			public Uri Source { get; private set; }

			public SerializedBitmap( Uri source )
			{
				Source = source;
			}
		}


		readonly VirtualDesktopManager _desktopManager;


		public ActivityDataContractSurrogate( VirtualDesktopManager desktopManager )
		{
			_desktopManager = desktopManager;
		}


		public Type GetDataContractType( Type type )
		{
			var convertTypes = new Dictionary<Type, Type>
			{
				{ typeof( ImageSource ), typeof( SerializedBitmap ) },
				{ typeof( SerializedBitmap ), typeof( BitmapImage ) },
				{ typeof( VirtualDesktop ), typeof( StoredSession ) },
				{ typeof( StoredSession ), typeof( VirtualDesktop ) }
			};

			return convertTypes.ContainsKey( type ) ? convertTypes[ type ] : type;
		}

		public object GetObjectToSerialize( object obj, Type targetType )
		{
			if ( targetType == typeof( SerializedBitmap ) )
			{
				return new SerializedBitmap( ((BitmapImage)obj).UriSource );
			}
			else if ( targetType == typeof( StoredSession ) )
			{
				return ((VirtualDesktop)obj).Store();
			}

			return obj;
		}

		public object GetDeserializedObject( object obj, Type targetType )
		{
			if ( targetType == typeof( ImageSource ) )
			{
				return new BitmapImage( ((SerializedBitmap)obj).Source );
			}
			else if ( targetType == typeof( VirtualDesktop ) )
			{
				return _desktopManager.CreateDesktopFromSession( (StoredSession)obj );
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
