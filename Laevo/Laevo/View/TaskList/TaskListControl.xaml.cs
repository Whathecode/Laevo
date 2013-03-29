using System;
using System.Windows;
using System.Windows.Input;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;
using Whathecode.System.Xaml.Behaviors;


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


		void OnTaskDragged( MouseBehavior.ClickDragInfo clickDragInfo )
		{
			DragDrop.DoDragDrop( clickDragInfo.Sender, clickDragInfo.Sender.DataContext, DragDropEffects.Move );
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

		void OnTaskDraggedPreview( object sender, MouseEventArgs e )
		{
			if ( e.LeftButton == MouseButtonState.Pressed )
			{
				var element = (FrameworkElement)sender;
				DragDrop.DoDragDrop( element, element.DataContext, DragDropEffects.Move );
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
