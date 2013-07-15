using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;


namespace Laevo.View.TaskList
{
	/// <summary>
	///   Interaction logic for TaskListControl.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	partial class TaskListControl
	{
		[Flags]
		public enum Properties
		{
			TaskHasFocus
		}


		[DependencyProperty( Properties.TaskHasFocus )]
		public bool TaskHasFocus { get; private set; }


		public TaskListControl()
		{
			InitializeComponent();
		}


		void OnTaskNameFocusChanged( object sender, DependencyPropertyChangedEventArgs e )
		{
			TaskHasFocus = (bool)e.NewValue;
		}

		void TaskNameKeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key.EqualsAny( Key.Enter, Key.Escape ) )
			{
				Common.ForceUpdateSource( e );
				e.Handled = true;
			}
		}

		ActivityViewModel _draggedTaskViewModel;
		void OnTaskDraggedPreview( object sender, MouseEventArgs e )
		{
			if ( e.LeftButton != MouseButtonState.Pressed )
			{
				return;
			}

			// Start the drag operation.
			var draggedTask = (FrameworkElement)sender;
			_draggedTaskViewModel = (ActivityViewModel)draggedTask.DataContext;
			DragDrop.DoDragDrop( draggedTask, draggedTask.DataContext, DragDropEffects.Move );

			// Finish the drag operation.
			_draggedTaskViewModel = null;
		}

		void OnDragTask( object sender, DragEventArgs e )
		{
			// Is it a task being dragged?
			var draggedTask = e.Data.GetData( typeof( ActivityViewModel ) ) as ActivityViewModel;
			if ( draggedTask == null )
			{
				return;
			}

			// Reposition tasks while dragging.
			Point currentPosition = e.GetPosition( Tasks );
			var viewModel = (ActivityOverviewViewModel)DataContext;
			ObservableCollection<ActivityViewModel> tasks = viewModel.Tasks;
			int draggedIndex = tasks.IndexOf( _draggedTaskViewModel );
			int currentIndex = (int)Math.Floor( currentPosition.X / ( Tasks.ActualWidth / tasks.Count ) );
			if ( draggedIndex != currentIndex )
			{
				viewModel.SwapTaskOrder( _draggedTaskViewModel, tasks[ currentIndex ] );
			}
		}

		void DragTaskFeedback( object sender, GiveFeedbackEventArgs e )
		{
			// TODO: Optionally hide default cursors in order to enable specialized visualizations.
			/*e.UseDefaultCursors = false;
			Mouse.SetCursor( Cursors.None );
			e.Handled = true;*/
		}
	}
}
