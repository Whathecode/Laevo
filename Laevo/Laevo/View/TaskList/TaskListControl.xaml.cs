﻿using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview;
using Whathecode.System.Arithmetic.Range;
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
			TaskHasFocus = 1
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
				Common.Actions.ForceUpdate( (TextBox)e.Source );
				e.Handled = true;
			}
		}

		ActivityViewModel _draggedTaskViewModel;

		void OnStartDrag( object sender, MouseEventArgs e )
		{
			if ( e.LeftButton != MouseButtonState.Pressed )
			{
				return;
			}

			// Start the drag operation.
			var draggedTask = (FrameworkElement)sender;
			_draggedTaskViewModel = (ActivityViewModel)draggedTask.DataContext;
			DragDrop.DoDragDrop( draggedTask, _draggedTaskViewModel, DragDropEffects.Move );

			// Finish the drag operation.
			_draggedTaskViewModel = null;
		}

		void OnReorderTasks( object sender, DragEventArgs e )
		{
			IgnoreDrop( sender, e );

			// Is it a task being reordered?
			var draggedTask = e.Data.GetData( typeof( ActivityViewModel ) ) as ActivityViewModel;
			var overview = (ActivityOverviewViewModel)DataContext;
			if ( draggedTask == null || !overview.Tasks.Contains( draggedTask ) )
			{
				return;
			}

			// Reposition tasks while dragging.
			Point currentPosition = e.GetPosition( Tasks );
			double clampedX = new Interval<double>( 0, Tasks.ActualWidth ).Clamp( currentPosition.X );
			var viewModel = (ActivityOverviewViewModel)DataContext;
			ReadOnlyObservableCollection<ActivityViewModel> tasks = viewModel.Tasks;
			int draggedIndex = tasks.IndexOf( _draggedTaskViewModel );
			int currentIndex = (int)Math.Floor( clampedX / ( Tasks.ActualWidth / tasks.Count ) );
			if ( draggedIndex != currentIndex )
			{
				viewModel.SwapTaskOrder( _draggedTaskViewModel, tasks[ currentIndex ] );
			}
		}

		void IgnoreDrop( object sender, DragEventArgs e )
		{
			e.Effects = DragDropEffects.None;
			e.Handled = true;
		}

		void OnTaskListDropOver( object sender, DragEventArgs e )
		{
			var activity = e.Data.GetData( typeof( ActivityViewModel ) ) as ActivityViewModel;
			if ( activity == null )
			{
				e.Effects = DragDropEffects.None;
			}
			else
			{
				var overview = (ActivityOverviewViewModel)DataContext;
				e.Effects = overview.Tasks.Contains( activity ) ? DragDropEffects.None : DragDropEffects.Move;
			}

			e.Handled = true;
		}

		void OnTaskListDrop( object sender, DragEventArgs e )
		{
			var activity = (ActivityViewModel)e.Data.GetData( typeof( ActivityViewModel ) );
			activity.Activity.MakeToDo();
		}

		void DragFeedback( object sender, GiveFeedbackEventArgs e )
		{
			if ( e.Effects == DragDropEffects.None )
			{
				Mouse.SetCursor( Cursors.No );
				e.Handled = true;
			}
		}
	}
}