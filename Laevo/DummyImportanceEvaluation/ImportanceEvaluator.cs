using System.Collections.Generic;
using System.Linq;
using ABC.Interruptions;


namespace DummyImportanceEvaluation
{
	public class ImportanceEvaluator
	{
		public static readonly IEnumerable<string> ImportantKeywords = new List<string>
		{
			"important",
			"immediately",
			"deadline",
			"now",
			"as soon as possible",
			"asap",
			"soon",
			"at once",
			"instant",
			"today",
			"earlier",
			"tomorrow",
			"still"
		};

		public static readonly string[] NotImportantKeywords =
		{
			"ago", "later", "next week"
		};

		static IEnumerable<string> SplitIntoWords( string text )
		{
			var punctuation = text.Where( char.IsPunctuation ).Distinct().ToArray();
			return text.Split().Select( x => x.Trim( punctuation ).ToLower() );
		}

		public static ImportanceLevel EvaluatieImportance( string notificationText )
		{
			var importance = ImportanceLevel.Low;
			var notificationWords = SplitIntoWords( notificationText );
			if ( notificationWords.Any( notificationWord => ImportantKeywords.Any( keyword => string.Equals( keyword, notificationWord ) ) ) )
			{
				importance = ImportanceLevel.High;
			}
			if ( notificationWords.Any( notificationWord => NotImportantKeywords.Any( keyword => keyword.Equals( notificationWords.ToString() ) ) ) )
			{
				importance = ImportanceLevel.Low;
			}
			return importance;
		}
	}
}