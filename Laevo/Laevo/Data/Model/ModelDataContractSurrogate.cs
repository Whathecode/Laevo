using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization;
using Laevo.Peer;


namespace Laevo.Data.Model
{
	public class ModelDataContractSurrogate : IDataContractSurrogate
	{
		[DataContract]
		class UsersPeerPlaceholder { }
		[DataContract]
		class RepositoryPlaceholder { }


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
				{ typeof( RepositoryPlaceholder ), typeof( IModelRepository ) }
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
			customDataTypes.Add( typeof( RepositoryPlaceholder) );
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
