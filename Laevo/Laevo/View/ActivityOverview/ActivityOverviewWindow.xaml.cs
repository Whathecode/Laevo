using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Laevo.View.Activity;
using Laevo.View.ActivityOverview.Converters;
using Laevo.View.ActivityOverview.Shaders;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview;
using Whathecode.System;
using Whathecode.System.Arithmetic;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;
using Whathecode.System.Windows.Input;
using Whathecode.System.Xaml.Behaviors;
using Whathecode.TimeLine;


namespace Laevo.View.ActivityOverview
{
	/// <summary>
	/// Interaction logic for ActivityOverviewWindow.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	public partial class ActivityOverviewWindow
	{
		[Flags]
		public enum Properties
		{
			MoveTimeLine,
			IsSchedulingActivity,
			WorkIntervals
		}


		public const double TopOffset = 105;
		public const double BottomOffset = 45;

		static readonly Interval<double> MaxPercentage = new Interval<double>( 0, 1 );
		const double ZoomPercentage = 0.001;
		const double DragMomentum = 0.0000001;

		[DependencyProperty( Properties.MoveTimeLine )]
		public ICommand MoveTimeLineCommand { get; private set; }

		[DependencyProperty( Properties.IsSchedulingActivity )]
		public bool IsSchedulingActivity { get; private set; }

		[DependencyProperty( Properties.WorkIntervals )]
		public ObservableCollection<WorkIntervalControl> WorkIntervals { get; private set; }


		public ActivityOverviewWindow()
		{
			InitializeComponent();

			MoveTimeLineCommand = new DelegateCommand<MouseBehavior.MouseDragCommandArgs>( MoveTimeLine );
			WorkIntervals = new ObservableCollection<WorkIntervalControl>();

#if DEBUG
			WindowStyle = WindowStyle.SingleBorderWindow;
			WindowState = WindowState.Normal;
			Topmost = false;
			Width = 1024;
			Height = 576;
#endif

			// Set the time line's position around the current time when first starting the application.
			DateTime now = DateTime.Now;
			var start = now - TimeSpan.FromHours( 1 );
			var end = now + TimeSpan.FromHours( 2 );
			TimeLine.VisibleInterval = new TimeInterval( start, end );

			CompositionTarget.Rendering += OnRendering;

			// Make sure no memory leaks occur when this window is unloaded.
			FadeContainer.Effect = new FadeEffect();
			Unloaded += ( sender, args ) =>
			{
				// Unhook events.
				CompositionTarget.Rendering -= OnRendering;

				// The fade effect is set and cleared through code-behind since it leaks otherwise:
				// https://connect.microsoft.com/VisualStudio/feedback/details/862878/pixelshader-holds-on-to-a-hard-reference-to-shadereffect-through-the-shaderbytecodechanged-event-creating-a-memory-leak-in-commen-shader-implementations
				FadeContainer.Effect = null;
			};
		}


		Interval<DateTime, TimeSpan> _startDrag;
		DateTime _startDragFocus;
		VisibleIntervalAnimation _dragAnimation;
		void MoveTimeLine( MouseBehavior.MouseDragCommandArgs info )
		{
			double mouseX = Mouse.GetPosition( this ).X;

			// Stop current time line animation.
			StopDragAnimation();

			if ( info.DragInfo.State == MouseBehavior.ClickDragState.Start )
			{
				_startDrag = TimeLine.VisibleInterval;
				_startDragFocus = GetFocusedTime( _startDrag, mouseX );
			}
			else if ( info.DragInfo.State == MouseBehavior.ClickDragState.Stop )
			{
				_startDrag = null;

				// Animate momentum when moving time line.
				long velocity = _velocity.GetCurrentRateOfChange();
				_dragAnimation = new VisibleIntervalAnimation
				{
					StartVelocity = velocity,
					ConstantDeceleration = velocity * DragMomentum
				};
				_dragAnimation.Completed += DragAnimationCompleted;
				TimeLine.BeginAnimation( TimeControl.VisibleIntervalProperty, _dragAnimation );
			}
			else
			{
				DateTime currentFocus = GetFocusedTime( _startDrag, mouseX );
				TimeSpan offset = currentFocus - _startDragFocus;
				if ( _startDrag.Start.Ticks - offset.Ticks > DateTime.MinValue.Ticks &&
					 _startDrag.End.Ticks - offset.Ticks < DateTime.MaxValue.Ticks )
				{
					TimeLine.VisibleInterval = _startDrag.Move( -offset );
				}
			}
		}

		DateTime GetFocusedTime( Interval<DateTime, TimeSpan> interval, double mouseXPosition )
		{
			// Find intersection of the ray along which is viewed, with the plane which shows the time line.
			double ratio = Container2D.ActualWidth / Container2D.ActualHeight;
			var leftFieldOfView = new Vector( -ratio, 0 );
			double angleRadians = MathHelper.DegreesToRadians( RotationTransform.Angle );
			var planeLine = new VectorLine( leftFieldOfView, new Vector( 0, Math.Tan( angleRadians ) * -ratio ) );
			double viewPercentage = mouseXPosition / Container2D.ActualWidth;
			double viewWidth = 2 * ratio;
			var viewRay = new VectorLine( new Vector( 0, 1 ), new Vector( -ratio + ( viewWidth * viewPercentage ), 0 ) );
			Vector viewIntersection = planeLine.Intersection( viewRay );

			// Find the percentage of the focused point on the time line.
			var rightFieldOfViewLine = new VectorLine( new Vector( 0, 1 ), new Vector( ratio, 0 ) );
			Vector rightIntersection = planeLine.Intersection( rightFieldOfViewLine );
			double planePercentage = leftFieldOfView.DistanceTo( viewIntersection ) / leftFieldOfView.DistanceTo( rightIntersection );
			planePercentage = MaxPercentage.Clamp( planePercentage );

			if ( planePercentage == 1.0 )
			{
				// Early out to prevent ArgumentOutOfRangeException due to rounding of double values.
				return interval.End;
			}
			return interval.GetValueAt( planePercentage );
		}

		void DragAnimationCompleted( object sender, EventArgs e )
		{
			StopDragAnimation();
		}

		void StopDragAnimation()
		{
			if ( _dragAnimation == null )
			{
				return;
			}

			_dragAnimation.Completed -= DragAnimationCompleted;
			TimeLine.VisibleInterval = TimeLine.VisibleInterval; // Required to copy latest animated value to local value.
			TimeLine.BeginAnimation( TimeControl.VisibleIntervalProperty, null );
			_dragAnimation = null;
		}

		static readonly Interval<DateTime, TimeSpan> MaxInterval = new Interval<DateTime, TimeSpan>( DateTime.MinValue, DateTime.MaxValue );
		void OnMouseWheel( object sender, MouseWheelEventArgs e )
		{
			StopDragAnimation();

			// Calculate which time is focused.
			Interval<DateTime, TimeSpan> visibleInterval = TimeLine.GetCoercedVisibleInterval();
			DateTime focusedTime = GetFocusedTime( visibleInterval, Mouse.GetPosition( this ).X );

			// Zoom the currently visible interval in/out.
			double zoom = 1.0 - ( e.Delta * ZoomPercentage );
			double focusPercentage = visibleInterval.GetPercentageFor( focusedTime );
			focusPercentage = MaxPercentage.Clamp( focusPercentage );
			try
			{
				TimeLine.VisibleInterval = visibleInterval.Scale( zoom, MaxInterval, focusPercentage );
				if ( visibleInterval.Size == TimeLine.GetCoercedVisibleInterval().Size ) // Prevent 'scrolling', rather than zooming when reaching minimum or maximum zoom level.
				{
					TimeLine.VisibleInterval = visibleInterval;
				}
			}
			catch ( ArgumentOutOfRangeException )
			{
				// This is rare, when at start or end of the interval. Simply ignore.
			}
		}

		readonly RateOfChange<long, long> _velocity = new RateOfChange<long, long>( TimeSpan.FromMilliseconds( 200 ).Ticks );

		void OnRendering( object sender, EventArgs e )
		{
			// While dragging, calculate velocity.
			if ( _startDrag != null )
			{
				_velocity.AddSample( TimeLine.VisibleInterval.Start.Ticks, DateTime.Now.Ticks );
			}
		}

		void OnTimeLineDragEnter( object sender, DragEventArgs e )
		{
			IsSchedulingActivity = true;

			HandleTimeLineDrag( e );
		}

		void OnTimeLineDragOver( object sender, DragEventArgs e )
		{
			HandleTimeLineDrag( e );
		}

		void OnTimeLineDragLeave( object sender, DragEventArgs e )
		{
			IsSchedulingActivity = false;

			HandleTimeLineDrag( e );
		}

		void HandleTimeLineDrag( DragEventArgs e )
		{
			UpdateFocusedTime( e.GetPosition( this ).X );

			// Update cursor.
			Point mouse = e.GetPosition( TimeLineContainer );
			double x = mouse.X;
			double y = mouse.Y;
			var currentTime = ((ActivityOverviewViewModel)DataContext).CurrentTime;
			double timePerc = TimeLine.VisibleInterval.GetPercentageFor( currentTime );
			double timeIndiciatorPosition = TimeLineContainer.ActualWidth * timePerc;
			if ( x <= timeIndiciatorPosition )
			{
				x -= DragDropCursor.ActualWidth;
			}
			y -= DragDropCursor.ActualHeight / 2;
			DragDropCursorPosition.SetValue( Canvas.LeftProperty, x );
			DragDropCursorPosition.SetValue( Canvas.TopProperty, y );

			// Set the allowed drop targets.
			var activity = e.Data.GetData( typeof( ActivityViewModel ) ) as ActivityViewModel;
			if ( activity == null )
			{
				e.Effects = DragDropEffects.None;
			}
			else
			{
				e.Effects = DragDropEffects.Move;
			}
			e.Handled = true;	
		}

		void OnTimeLineDragDropped( object sender, DragEventArgs e )
		{
			UpdateFocusedTime( e.GetPosition( this ).X );
			UpdateOffsetPercentage( e.GetPosition( TimeLineContainer ).Y );
			IsSchedulingActivity = false;

			var activity = (ActivityViewModel)e.Data.GetData( typeof( ActivityViewModel ) );
			var overview = (ActivityOverviewViewModel)DataContext;

			overview.ActivityDropped( activity );
		}

		void OnContextMenuOpening( object sender, ContextMenuEventArgs e )
		{
			UpdateFocusedTime( Mouse.GetPosition( this ).X );
			UpdateOffsetPercentage( Mouse.GetPosition( TimeLineContainer ).Y );
		}

		void UpdateFocusedTime( double mouseX )
		{
			var overview = (ActivityOverviewViewModel)DataContext;

			DateTime focusedTime = GetFocusedTime( TimeLine.VisibleInterval, mouseX );
			overview.FocusedTimeChanged( focusedTime );
		}

		void UpdateOffsetPercentage( double mouseY )
		{
			var overview = (ActivityOverviewViewModel)DataContext;

			var percentageInterval = new Interval<double>( 0, 1 );
			var activitySpace = new Interval<double>( TimeLineContainer.ActualHeight - BottomOffset, TopOffset );
			overview.FocusedOffsetPercentage = activitySpace.Map( mouseY, percentageInterval );
		}

		void OnHomeDropOver( object sender, DragEventArgs e )
		{
			var activity = e.Data.GetData( typeof( ActivityViewModel ) ) as ActivityViewModel;
			if ( activity == null )
			{
				e.Effects = DragDropEffects.None;
			}

			e.Handled = true;
		}

		void OnHomeDrop( object sender, DragEventArgs e )
		{
			var activity = (ActivityViewModel)e.Data.GetData( typeof( ActivityViewModel ) );
			var overview = (ActivityOverviewViewModel)DataContext;

			overview.HomeActivity.Merge( activity );
		}
	}
}