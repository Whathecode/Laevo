using System.Collections.Generic;
using System.IO;
using System.Linq;
using ABC.Interruptions;


namespace NotificationManager
{
	public class RealationDetector
	{
		/// <summary>
		/// When the similarity level is above this value, an activity and interruption are considered to be related.
		/// </summary>
		public const int Threshold = 51;

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
		/// <returns>True if interruption and activity are potentially similar, false otherwise.</returns>
		public static bool CheckIfRelted( AbstractInterruption interruption, string activityName, List<string> participantNames = null, string activityFolderPath = null )
		{
			var interruptionNameWords = interruption.Name.Split( ' ' ).Select( word => word.ToLower().Trim() ).ToList();
			var activityNameWords = activityName.Split( ' ' ).Select( word => word.ToLower().Trim() ).ToList();

			var simlarity = Compare( interruptionNameWords, activityNameWords );
			if ( simlarity < Threshold )
			{
				if (participantNames != null &&  participantNames.Count != 0)
				{
					simlarity = Compare( interruption.Collaborators, participantNames );
				}
			}
			if ( simlarity < Threshold )
			{
				if ( !string.IsNullOrEmpty( activityFolderPath ) )
				{
					var filesInActivityLibrary = Directory.GetFiles( activityFolderPath );
					simlarity = Compare( interruption.Files, filesInActivityLibrary );
				}
			}
			return simlarity > Threshold;
		}
	}
}
