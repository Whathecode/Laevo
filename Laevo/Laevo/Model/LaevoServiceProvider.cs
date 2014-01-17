using System.ComponentModel.Composition;
using ABC.Applications.Services;


namespace Laevo.Model
{
	[Export( typeof( ServiceProvider ) )]
	public class LaevoServiceProvider : ServiceProvider
	{
		public LaevoServiceProvider()
		{
			ForceOpenInNewWindow = true;
		}
	}
}
