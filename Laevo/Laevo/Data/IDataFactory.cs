using ABC.Windows.Desktop;
using Laevo.Data.Model;
using Laevo.Data.View;


namespace Laevo.Data
{
	/// <summary>
	///   A factory which creates concrete data repositories.
	/// </summary>
	/// <author>Steven Jeuris</author>
	interface IDataFactory
	{
		IModelRepository CreateModelRepository();
		IViewRepository CreateViewRepository( IModelRepository linkedModelRepository, VirtualDesktopManager desktopManager );
	}
}
