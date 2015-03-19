using System;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Laevo.ViewModel.Activity;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Controls;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;
using Whathecode.System.Windows.Input;
using Whathecode.System.Windows.Media.Extensions;
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
		public DelegateCommand<MouseBehavior.MouseDragCommandArgs> MouseDragged { get; private set; }

		[DependencyProperty( Properties.IsDraggingActivity )]
		public bool IsDraggingActivity { get; private set; }

		static Interval<double> _percentageInterval = new Interval<double>( 0, 1 );

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

				// HACK: Refresh binding so accurate attention span lines for planned activities are shown. They would otherwise not be redrawn when open.
				if ( dataContext.BaseActivity.IsPlanned )
				{
					_updateTimer.Elapsed += ( sender, args ) => ActiveItemsControl.Dispatcher.BeginInvoke( DispatcherPriority.Background, new Action( () =>
					{
						if ( dataContext.ShowActiveTimeSpans && dataContext.BaseActivity.IsActive )
						{
							// To re-evaluate ItemsSource of ActiveItemsControl which is bound to a trigger,
							// the value to which the trigger is bound is reset.
							dataContext.ShowActiveTimeSpans = false;
							dataContext.ShowActiveTimeSpans = true;
						}
					} ) );
					_updateTimer.Start();
				}
			};

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

			var parent = this.FindParent<TimePanel>();
			double displacement = e.DragInfo.Displacement.Y / parent.ActualHeight;
			var viewModel = (WorkIntervalViewModel)DataContext;
			double offset = viewModel.OffsetPercentage;
			viewModel.OffsetPercentage = _percentageInterval.Clamp( offset - displacement );
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
			var dropTarget = ((WorkIntervalViewModel)DataContext).BaseActivity;
			if ( !CanDrop( activity, dropTarget ) )
			{
				e.Effects = DragDropEffects.None;
			}

			e.Handled = true;
		}

		void OnDrop( object sender, DragEventArgs e )
		{
			var activity = (ActivityViewModel)e.Data.GetData( typeof( ActivityViewModel ) );
			var dropTarget = ((WorkIntervalViewModel)DataContext).BaseActivity;

			if ( CanDrop( activity, dropTarget ) )
			{
				dropTarget.Merge( activity );
			}

			e.Handled = true;
		}

		bool CanDrop( ActivityViewModel toMerge, ActivityViewModel dropTarget )
		{
			return toMerge != null && dropTarget.IsAccessible;
		}

		void StartDrag( object sender, MouseEventArgs e )
		{
			var activity = (WorkIntervalViewModel)DataContext;

			if ( e.LeftButton == MouseButtonState.Pressed && !activity.HasMoreRecentRepresentation && activity.BaseActivity.IsAccessible )
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