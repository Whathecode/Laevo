﻿using System;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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
	public partial class WorkIntervalControl
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

		/// <summary>
		///   Timer used to update active time spans.
		/// </summary>
		readonly Timer _updateTimer = new Timer( 100 );

		public WorkIntervalControl()
		{
			InitializeComponent();
			DataContextChanged += ( s, a ) =>
			{
				var dataContext = (WorkIntervalViewModel)DataContext;

				// Hack- Additional binding refresh to show active time spans for planned activities (they are not redrawn when are opened).
				if ( dataContext.BaseActivity.IsPlanned )
				{
					// Hook up timer.
					_updateTimer.Elapsed += ( sender, args ) => ActiveItemsControl.Dispatcher.BeginInvoke( DispatcherPriority.Background, new Action( () =>
					{
						if ( dataContext.ShowActiveTimeSpans && dataContext.BaseActivity.IsActive )
						{
							// To reevaluate ActiveItemsControl itemsSource which is boud to trigger I reset the value bound to that trigger.
							dataContext.ShowActiveTimeSpans = false;
							dataContext.ShowActiveTimeSpans = true;
						}
					} ) );
					_updateTimer.Start();
				}
			};

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
				Common.Actions.ForceUpdate( ActivityName );
			}
		}

		void OnMouseMoved( object sender, MouseEventArgs e )
		{
			double xOffset = e.GetPosition( this ).X - Buttons.ActualWidth / 2;
			Buttons.Margin = new Thickness( xOffset, Container.ActualHeight, 0, 0 );
		}

		void OnDropOver( object sender, DragEventArgs e )
		{
			var activity = e.Data.GetData( typeof( ActivityViewModel ) ) as ActivityViewModel;
			if ( activity == null )
			{
				e.Effects = DragDropEffects.None;
			}

			e.Handled = true;
		}

		void OnDrop( object sender, DragEventArgs e )
		{
			var activity = (ActivityViewModel)e.Data.GetData( typeof( ActivityViewModel ) );
			var dropTarget = (WorkIntervalViewModel)DataContext;

			dropTarget.BaseActivity.Merge( activity );

			e.Handled = true;
		}

		void StartDrag( object sender, MouseEventArgs e )
		{
			var activity = (WorkIntervalViewModel)DataContext;

			if ( e.LeftButton == MouseButtonState.Pressed && !activity.HasMoreRecentRepresentation )
			{
				var draggedTask = (FrameworkElement)sender;
				var draggedActivity = activity.BaseActivity;
				DragDrop.DoDragDrop( draggedTask, draggedActivity, DragDropEffects.Move );
			}
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