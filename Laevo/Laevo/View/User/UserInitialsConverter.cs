using System;
using System.Linq;
using System.Text.RegularExpressions;
using Whathecode.System.Windows.Data;


namespace Laevo.View.User
{
	/// <summary>
	///   Converts a user name to representative initials.
	/// </summary>
	class UserInitialsConverter : AbstractValueConverter<string, string>
	{
		public override string Convert( string value )
		{
			value = ( value ?? "" );
			value = Regex.Replace(value, @"\s+", " ");
			value = value.Trim();
			if ( value.Length == 0 )
			{
				return "-";
			}

			char[] initials = value.Split( ' ' ).Select( s => Char.ToUpper( s[ 0 ] ) ).ToArray();
			return initials.Length > 1
				? new string( new [] { initials[ 0 ], initials[ initials.Length - 1 ] } )
				: initials[ 0 ].ToString();
		}

		public override string ConvertBack( string value )
		{
			throw new NotSupportedException();
		}
	}
}
