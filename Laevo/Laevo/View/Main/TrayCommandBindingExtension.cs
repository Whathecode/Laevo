using System.Windows.Input;
using System.Windows.Markup;
using Whathecode.System.Algorithm;
using Whathecode.System.Windows.Input.CommandFactory;


namespace Laevo.View.Main
{
	/// <summary>
	///   Custom command binding, used to skip the first time the DataContext is set.
	///   This is required since TaskBarIcon sets its DataContext itself.
	/// </summary>
	[MarkupExtensionReturnType( typeof( ICommand ) )]
	public class TrayCommandBindingExtension : CommandBindingExtension
	{
		readonly IGate _skipFirst = new SkipGate( 1 );


		public TrayCommandBindingExtension( object commandId )
			: base( commandId ) {}


		protected override object ProvideValue( object dataContext )
		{
			return _skipFirst.TryEnter() ? base.ProvideValue( dataContext ) : null;
		}
	}
}
