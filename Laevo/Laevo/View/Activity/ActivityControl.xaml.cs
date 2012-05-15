using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Laevo.View.ActivityOverview;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;
using Whathecode.System.Windows.Input;
using Whathecode.System.Xaml.Behaviors;


namespace Laevo.View.Activity
{	
	/// <summary>
	/// Interaction logic for ActivityControl.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	public partial class ActivityControl
	{
		[Flags]
		public enum Properties
		{
			MouseDragged
		}

		[DependencyProperty( Properties.MouseDragged )]
		public DelegateCommand<MouseBehavior.ClickDragInfo> MouseDragged { get; private set; }

		
		public ActivityControl()
		{
			InitializeComponent();

			MouseDragged = new DelegateCommand<MouseBehavior.ClickDragInfo>( MoveActivity );
		}


		void MoveActivity( MouseBehavior.ClickDragInfo e )
		{
			double offset = (double)GetValue( TimeLineControl.OffsetProperty );
			SetValue( TimeLineControl.OffsetProperty, offset - e.Displacement.Y );
		}

		void LabelKeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key.EqualsAny( Key.Enter, Key.Escape ) )
			{
				ForceUpdateSource( e );
			}
		}

		void OnStartEditActivity( object sender, MouseButtonEventArgs e )
		{
			ForceUpdateSource( e );
		}

		static void ForceUpdateSource( RoutedEventArgs e )
		{			
			var element = (UIElement)e.Source;

			// Moving focus also updates the source.
			element.MoveFocus( new TraversalRequest( FocusNavigationDirection.Previous ) );
		}
	}
}
