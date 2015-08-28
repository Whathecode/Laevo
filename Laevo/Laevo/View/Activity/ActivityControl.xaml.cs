using System;
using System.Windows;
using System.Windows.Input;
using Laevo.View.ActivityOverview;
using Laevo.ViewModel.Activity;
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
			MouseDragged,
			IsDraggingActivity
		}

		[DependencyProperty( Properties.MouseDragged )]
		public DelegateCommand<MouseBehavior.MouseDragCommandArgs> MouseDragged { get; private set; }

		[DependencyProperty( Properties.IsDraggingActivity )]
		public bool IsDraggingActivity { get; private set; }


		public ActivityControl()
		{
			InitializeComponent();

			MouseDragged = new DelegateCommand<MouseBehavior.MouseDragCommandArgs>( MoveActivity );
		}


		void MoveActivity( MouseBehavior.MouseDragCommandArgs e )
		{
			if ( e.DragInfo.State == MouseBehavior.ClickDragState.Start )
			{
				IsDraggingActivity = true;
			}
			else if ( e.DragInfo.State == MouseBehavior.ClickDragState.Stop )
			{
				IsDraggingActivity = false;
			}

			var offset = (double)GetValue( TimeLineControl.OffsetProperty );
			SetValue( TimeLineControl.OffsetProperty, offset - e.DragInfo.Displacement.Y );
		}

		void LabelKeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key.EqualsAny( Key.Enter, Key.Escape ) )
			{
				Common.ForceUpdate( ActivityName );
			}
		}

		void OnMouseMoved( object sender, MouseEventArgs e )
		{
			double xOffset = e.GetPosition( this ).X - Buttons.ActualWidth / 2;
			Buttons.Margin = new Thickness( xOffset, Container.ActualHeight, 0, 0 );
		}

		void OnTaskDropped( object sender, DragEventArgs e )
		{
			var draggedTask = e.Data.GetData( typeof( ActivityViewModel ) ) as ActivityViewModel;
			if ( draggedTask == null )
			{
				return;
			}

			var dropTarget = (ActivityViewModel)DataContext;
			dropTarget.Merge( draggedTask );
		}
	}
}
