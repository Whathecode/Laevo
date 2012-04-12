using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;


namespace Laevo.Model.AttentionShifts
{
	class DataContractSurrogate : IDataContractSurrogate
	{
		[DataContract]
		class SerializedActivity
		{
			[DataMember]
			public DateTime DateCreated { get; private set; }

			public SerializedActivity( DateTime dateCreated )
			{
				DateCreated = dateCreated;
			}
		}


		readonly List<Activity> _activities;


		public DataContractSurrogate( List<Activity> activities )
		{
			_activities = activities;
		}


		public Type GetDataContractType( Type type )
		{
			var convertTypes = new Dictionary<Type, Type>
			{
				{ typeof( Activity ), typeof( SerializedActivity ) },
				{ typeof( SerializedActivity ), typeof( Activity ) }
			};

			return convertTypes.ContainsKey( type ) ? convertTypes[ type ] : type;
		}

		public object GetObjectToSerialize( object obj, Type targetType )
		{
			if ( targetType == typeof( SerializedActivity ) )
			{
				return new SerializedActivity( ((Activity)obj).DateCreated );
			}

			return obj;
		}

		public object GetDeserializedObject( object obj, Type targetType )
		{
			SerializedActivity activity = obj as SerializedActivity;
			if ( activity != null )
			{
				return _activities.First( a => a.DateCreated == activity.DateCreated );
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
