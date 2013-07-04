using System;
using System.ComponentModel.Composition;
using Laevo.Model.Interruptions;


namespace Laevo.InterruptionHandlers
{
	/// <summary>
	///   Receives unread emails from the currently logged in gmail account and introduces them as interruptions.
	/// </summary>
	[Export( typeof( AbstractInterruptionHandler ) )]
	public class Gmail : AbstractInterruptionHandler
	{
		public override void Update( DateTime now )
		{
			// TODO: Get syndication feed from https://mail.google.com/mail/feed/atom and introduce interruptions for new emails.
		}
	}
}
