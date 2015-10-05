using System.Collections.Generic;
using System.IO;
using System.Linq;
using ABC.Interruptions;
using Laevo.Model;
using Whathecode.System.Extensions;


namespace Laevo.Interruptions
{
	class InterruptionActivityCompare
	{
		/// <summary>
		/// When the similarity level is above this value, an activity and interruption are considered to be related.
		/// </summary>
		public const int SimilarityLevel = 51;

		static double Compare( List<string> fitstList, ICollection<string> secondList )
		{
			double wordOccrenceCount = 0;
			fitstList.ForEach( word =>
			{
				if ( secondList.Contains( word ) )
				{
					wordOccrenceCount++;
				}
			} );

			return wordOccrenceCount / fitstList.Count * 100;
		}

		/// <summary>
		/// Compares an interruption with an activity, checks for any potential relation.
		/// </summary>
		/// <param name="interruption">Interruption to compare.</param>
		/// <param name="activity">Activity to compare.</param>
		/// <returns>True if interruption and activity are potentially similar, false otherwise.</returns>
		public static bool CheckIfRelted( AbstractInterruption interruption, Activity activity )
		{
			var interruptionNameWords = interruption.Name.Split( ' ' ).Select( word => word.ToLower().Trim() ).ToList();
			var activityNameWords = activity.Name.Split( ' ' ).Select( word => word.ToLower().Trim() ).ToList();

			var simlarity = Compare( interruptionNameWords, activityNameWords );
			if ( simlarity < SimilarityLevel )
			{
				var userNames = new List<string>();
				activity.AccessUsers.Concat( activity.OwnedUsers ).ForEach( user => userNames.Add( user.Name ) );
				simlarity = Compare( interruption.Collaborators, userNames );
			}
			if ( simlarity < SimilarityLevel )
			{
				var filesInActivityLibrary = Directory.GetFiles( activity.SpecificFolder.AbsolutePath );
				simlarity = Compare( interruption.Files, filesInActivityLibrary );
			}
			return !( simlarity < SimilarityLevel );
		}
	}
}