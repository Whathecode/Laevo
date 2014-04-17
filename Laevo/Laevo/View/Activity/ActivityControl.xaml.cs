using System;
using System.Windows;
using System.Windows.Input;
using Laevo.View.ActivityOverview;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.Activity.LinkedActivity;
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
		public DelegateCommand<MouseBehavior.ClickDragInfo> MouseDragged { get; private set; }

		[DependencyProperty( Properties.IsDraggingActivity )]
		public bool IsDraggingActivity { get; private set; }


		public ActivityControl()
		{
			InitializeComponent();

			MouseDragged = new DelegateCommand<MouseBehavior.ClickDragInfo>( MoveActivity );
		}


		void MoveActivity( MouseBehavior.ClickDragInfo e )
		{
			if ( e.State == MouseBehavior.ClickDragState.Start )
			{
				IsDraggingActivity = true;
			}
			else if ( e.State == MouseBehavior.ClickDragState.Stop )
			{
				IsDraggingActivity = false;
			}

			double offset = (double)GetValue( TimeLineControl.OffsetProperty );
			SetValue( TimeLineControl.OffsetProperty, offset - e.Displacement.Y );
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

			var dropTarget = (LinkedActivityViewModel)DataContext;
			dropTarget.BaseActivity.Merge( draggedTask );
		}

		void CreateToDoItem( object sender, MouseEventArgs e )
		{
			var linkedActivity = (LinkedActivityViewModel)DataContext;
			if ( e.LeftButton == MouseButtonState.Pressed
				// Creation of to do item should be possible only using the newest linked activity.
			     && linkedActivity.BaseActivity.LinkedActivities.Count == linkedActivity.BaseActivity.LinkedActivities.IndexOf( linkedActivity ) + 1 )
			{
				StartDrag( sender );
			}
		}

		void ReschedulePlanned( object sender, MouseEventArgs e )
		{
			// Second condition disables rescheduling planned activity which lays already in the past.
			var linkedActivity = (LinkedActivityViewModel)DataContext;
			if ( e.LeftButton == MouseButtonState.Pressed && ( DateTime.Now < linkedActivity.Occurance && linkedActivity.IsPlanned ) )
			{
				StartDrag( sender );
			}
		}

		void StartDrag( object sender )
		{
			var draggedTask = (FrameworkElement)sender;
			DragDrop.DoDragDrop( draggedTask, DataContext, DragDropEffects.Move );
		}
	}
}