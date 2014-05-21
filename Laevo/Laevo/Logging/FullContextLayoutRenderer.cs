using System;
using System.Linq;
using System.Text;
using NLog;
using NLog.LayoutRenderers;


namespace Laevo.Logging
{
	[LayoutRenderer( "fullcontext" )]
	class FullContextLayoutRenderer : LayoutRenderer
	{
		protected override void Append( StringBuilder builder, LogEventInfo logEvent )
		{
			int count = logEvent.Properties.Count;
			if ( count == 0 )
			{
				return;
			}

			string context = "[ " + String.Join( ", ", logEvent.Properties.Select( kv => kv.Key + " = " + kv.Value ) ) + " ]";
			builder.Append( context);
		}
	}
}
