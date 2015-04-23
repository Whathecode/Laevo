using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using Laevo.Peer;


namespace Laevo.Data.Model
{
	public class ModelDataContractSurrogate : IDataContractSurrogate
	{
		[DataContract]
		class UsersPeerPlaceholder { }


		readonly IUsersPeer _usersPeer;


		public ModelDataContractSurrogate( IUsersPeer usersPeer )
		{
			_usersPeer = usersPeer;
		}


		public Type GetDataContractType( Type type )
		{
			var convertTypes = new Dictionary<Type, Type>
			{
				{ typeof( UsersPeerPlaceholder ), typeof( IUsersPeer ) }
			};

			return convertTypes.ContainsKey( type ) ? convertTypes[ type ] : type;
		}

		public object GetObjectToSerialize( object obj, Type targetType )
		{
			if ( targetType == typeof( IUsersPeer ) )
			{
				return new UsersPeerPlaceholder();
			}

			return obj;
		}

		public object GetDeserializedObject( object obj, Type targetType )
		{
			if ( targetType == typeof( IUsersPeer ) )
			{
				return _usersPeer;
			}

			return obj;
		}

		public object GetCustomDataToExport( MemberInfo memberInfo, Type dataContractType )
		{
			return null;
		}

		public object GetCustomDataToExport( Type clrType, Type dataContractType )
		{
			return null;;
		}

		public void GetKnownCustomDataTypes( Collection<Type> customDataTypes )
		{
			customDataTypes.Add( typeof( UsersPeerPlaceholder ) );
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
