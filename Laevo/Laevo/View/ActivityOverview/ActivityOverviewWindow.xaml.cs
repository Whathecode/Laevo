using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Laevo.View.Activity;
using Laevo.View.ActivityOverview.Converters;
using Laevo.View.ActivityOverview.Labels;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview;
using Whathecode.System;
using Whathecode.System.Algorithm;
using Whathecode.System.Arithmetic;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Collections.Generic;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;
using Whathecode.System.Windows.Input;
using Whathecode.System.Xaml.Behaviors;


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
			IsTimeLineDraggedOver
		}


		public const double TopOffset = 105;
		public const double BottomOffset = 45;

		const double ZoomPercentage = 0.001;
		const double DragMomentum = 0.0000001;

		readonly List<UnitLabels> _unitLabels = new List<UnitLabels>(); 
		readonly List<ILabels> _labels = new List<ILabels>();
		readonly Dictionary<ActivityViewModel, ActivityControl> _activities = new Dictionary<ActivityViewModel, ActivityControl>();

		[DependencyProperty( Properties.MoveTimeLine )]
		public ICommand MoveTimeLineCommand { get; private set; }

		[DependencyProperty( Properties.IsTimeLineDraggedOver )]
		public bool IsTimeLineDraggedOver { get; private set; }

		readonly TimeIndicator _timeIndicator;


		public ActivityOverviewWindow()
		{
			InitializeComponent();

			MoveTimeLineCommand = new DelegateCommand<MouseBehavior.ClickDragInfo>( MoveTimeLine );

#if DEBUG
			WindowStyle = WindowStyle.None;
			WindowState = WindowState.Normal;
			Topmost = false;
			Width = 1280;
			Height = 720;
#endif

			// Set the time line's position around the current time when first starting the application.
			DateTime now = DateTime.Now;
			var start = now - TimeSpan.FromHours( 1 );
			var end = now + TimeSpan.FromHours( 2 );
			TimeLine.VisibleInterval = new Interval<DateTime>( start, end );

			// Create the line which indicates the current time.
			_timeIndicator = new TimeIndicator { Width = 20 };
			_timeIndicator.SetBinding( HeightProperty, new Binding( "ActualHeight" ) { Source = TimeLine } );
			_timeIndicator.SetBinding( TimeLineControl.OccuranceProperty, "CurrentTime" );
			TimeLine.Children.Add( _timeIndicator );

			// Create desired intervals to show.
			// TODO: This logic seems abstract enough to move to the model.
			// TODO: Prevent showing dates outside of a certain scope to prevent exceptions.
			var months = new IrregularInterval( TimeSpanHelper.MinimumMonthLength, DateTimePart.Month, d => d.AddMonths( 1 ) );
			var years = new IrregularInterval( TimeSpanHelper.MinimumYearLength, DateTimePart.Year, d => d.AddYears( 1 ) );
			var weeks = new RegularInterval(
				d => d.Round( DateTimePart.Day ) - TimeSpan.FromDays( (int)d.DayOfWeek == 0 ? 6 : (int)d.DayOfWeek - 1 ),
				TimeSpan.FromDays( 7 ) );
			var days = new RegularInterval( 1, DateTimePart.Day );
			var everySixHours = new RegularInterval( 6, DateTimePart.Hour );
			var hours = new RegularInterval( 1, DateTimePart.Hour );
			var quarters = new RegularInterval( 15, DateTimePart.Minute );

			// Create vertical interval lines.
			var labelList = new TupleList<IInterval, Func<DateTime, bool>>
			{
				{ years, d => true },
				{ months, d => d.Month != 1 },
				{ weeks, d => d.Day != 1 },
				{ days, d => d.DayOfWeek != DayOfWeek.Monday && d.Day != 1 },
				{ everySixHours, d => d.Hour.EqualsAny( 6, 12, 18 ) },
				{ hours, d => d.Hour != 0 && !d.Hour.EqualsAny( 6, 12, 18 ) },
				{ quarters, d => d.Minute != 0 }
			};
			labelList.ForEach( l => _labels.Add( new TimeSpanLabels( TimeLine, l.Item1, l.Item2 ) ) );

			// Create unit labels near interval lines.
			var quarterUnits = new UnitLabels( TimeLine, quarters, "HH:mm", () => true );
			_unitLabels.Add( quarterUnits );
			var hourUnits = new UnitLabels( TimeLine, hours, "HH:mm", () => !quarterUnits.LabelsFitScreen() );
			_unitLabels.Add( hourUnits );
			var sixHourUnits = new UnitLabels( TimeLine, everySixHours, "HH:mm", () => !hourUnits.LabelsFitScreen() );
			_unitLabels.Add( sixHourUnits );
			var dayUnits = new UnitLabels( TimeLine, days, @"d\t\h", () => !sixHourUnits.LabelsFitScreen() );
			_unitLabels.Add( dayUnits );
			var dayNameUnits = new UnitLabels( TimeLine, days, "dddd", () => !sixHourUnits.LabelsFitScreen(), 25, 18 );
			_unitLabels.Add( dayNameUnits );
			var dayCompleteUnits = new UnitLabels( TimeLine, days, @"dddd d\t\h", () => sixHourUnits.LabelsFitScreen() && dayUnits.LabelsFitScreen(), 25, 18 );
			_unitLabels.Add( dayCompleteUnits );
			var weekUnits = new UnitLabels( TimeLine, weeks, @"d\t\h", () => !dayUnits.LabelsFitScreen() );
			_unitLabels.Add( weekUnits );
			var monthSmallUnits = new UnitLabels( TimeLine, months, "MMMM", () => !dayUnits.LabelsFitScreen() && weekUnits.LabelsFitScreen(), 25, 30 );
			_unitLabels.Add( monthSmallUnits );
			var monthUnits = new UnitLabels( TimeLine, months, "MMMM", () => !weekUnits.LabelsFitScreen() );
			_unitLabels.Add( monthUnits );
			_labels.AddRange( _unitLabels );

			// Add header labels.
			var headerLabels = new HeaderLabels( TimeLine );
			headerLabels.AddInterval( years, "yyyy" );
			headerLabels.AddInterval( months, "MMMM" );
			headerLabels.AddInterval(
				weeks,
				d => "Week " + CultureInfo.CurrentCulture.Calendar.GetWeekOfYear( d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday ) );
			headerLabels.AddInterval( days, @"dddd d\t\h" );
			headerLabels.AddInterval(
				everySixHours,
				d => d.Hour == 0 ? "Midnight" : d.Hour == 6 ? "Morning" : d.Hour == 12 ? "Noon" : "Evening" );
			headerLabels.AddInterval( hours, "H:00" );
			headerLabels.AddInterval( quarters, "HH:mm" );
			_labels.Add( headerLabels );

			// Add breadcrumb labels.
			var breadcrumbs = new BreadcrumbLabels( TimeLine );
			breadcrumbs.AddInterval( years, "yyyy" );
			breadcrumbs.AddInterval( months, "yyyy" );
			breadcrumbs.AddInterval( weeks, "Y" );
			breadcrumbs.AddInterval( days, "Y" );
			breadcrumbs.AddInterval( everySixHours, "D" );
			breadcrumbs.AddInterval( hours, "D" );
			breadcrumbs.AddInterval( quarters, "D" );
			_labels.Add( breadcrumbs );

			// Hook up all labels to listen to time line changes.
			_labels.Select( l => l.Labels ).ForEach( l => l.CollectionChanged += ( s, e ) =>
			{
				switch ( e.Action )
				{
					case NotifyCollectionChangedAction.Add:
						e.NewItems.Cast<FrameworkElement>().ForEach( i => TimeLine.Children.Add( i ) );
						break;

					case NotifyCollectionChangedAction.Remove:
						e.OldItems.Cast<FrameworkElement>().ForEach( i => TimeLine.Children.Remove( i ) );
						break;
				}
			} );
			Action updatePositions = () => _labels.ForEach( l => l.UpdatePositions() );
			TimeLine.VisibleIntervalChangedEvent += i => updatePositions();
			var widthDescriptor = DependencyPropertyDescriptor.FromProperty( ActualWidthProperty, typeof( TimeLineControl ) );
			widthDescriptor.AddValueChanged( TimeLine, ( s, e ) => updatePositions() );

			DataContextChanged += NewDataContext;
			CompositionTarget.Rendering += OnRendering;
		}


		void NewDataContext( object sender, DependencyPropertyChangedEventArgs e )
		{
			var oldViewModel = e.OldValue as ActivityOverviewViewModel;
			if ( oldViewModel != null )
			{
				oldViewModel.Activities.CollectionChanged -= ActivitiesChanged;
			}
		
			var viewModel = e.NewValue as ActivityOverviewViewModel;
			if ( viewModel == null )
			{
				return;
			}
			viewModel.Activities.ForEach( NewActivity );
			viewModel.Activities.CollectionChanged += ActivitiesChanged;
		}

		void ActivitiesChanged( object sender, NotifyCollectionChangedEventArgs e )
		{
			// Remove old items.
			if ( e.OldItems != null )
			{
				foreach ( var activity in e.OldItems.Cast<ActivityViewModel>() )
				{
					TimeLine.Children.Remove( _activities[ activity ] );
					_activities.Remove( activity );
				}
			}

			// Add new items.
			if ( e.NewItems != null )
			{
				e.NewItems.Cast<ActivityViewModel>().ForEach( NewActivity );
			}
		}

		void NewActivity( ActivityViewModel viewModel )
		{
			var activity = new ActivityControl
			{
				DataContext = viewModel,
				HorizontalAlignment = HorizontalAlignment.Left,
			};
			_activities.Add( viewModel, activity );
			TimeLine.Children.Add( activity );
		}

		Interval<DateTime> _startDrag;
		DateTime _startDragFocus;
		VisibleIntervalAnimation _dragAnimation;
		void MoveTimeLine( MouseBehavior.ClickDragInfo info )
		{
			double mouseX = Mouse.GetPosition( this ).X;

			// Stop current time line animation.
			DependencyProperty visibleIntervalProperty = TimeLine.GetDependencyProperty( TimeLineControl.Properties.VisibleInterval );
			StopDragAnimation();

			if ( info.State == MouseBehavior.ClickDragState.Start )
			{
				_startDrag = TimeLine.VisibleInterval;
				_startDragFocus = GetFocusedTime( _startDrag, mouseX );
			}
			else if ( info.State == MouseBehavior.ClickDragState.Stop )
			{
				_startDrag = null;

				// Animate momentum when moving time line.
				long velocity = _velocity.GetCurrentRateOfChange();
				_dragAnimation = new VisibleIntervalAnimation
				{
					StartVelocity = velocity,
					ConstantDeceleration = velocity * DragMomentum,
					TimeLine = TimeLine					
				};
				_dragAnimation.Completed += DragAnimationCompleted;	
				TimeLine.BeginAnimation( visibleIntervalProperty, _dragAnimation );
			}
			else
			{
				DateTime currentFocus = GetFocusedTime( _startDrag, mouseX );
				var ticksOffset = currentFocus.Ticks - _startDragFocus.Ticks;
				var interval = ToTicksInterval( _startDrag );
				if ( interval.Start - ticksOffset > DateTime.MinValue.Ticks && interval.End - ticksOffset < DateTime.MaxValue.Ticks )
				{
					interval.Move( -ticksOffset );
					TimeLine.VisibleInterval = ToTimeInterval( interval );
				}
			}
		}

		DateTime GetFocusedTime( Interval<DateTime> interval, double mouseXPosition )
		{
			Interval<long> ticksInterval = ToTicksInterval( interval );

			// Find intersection of the ray along which is viewed, with the plane which shows the time line.
			double ratio = Container2D.ActualWidth / Container2D.ActualHeight;
			Vector leftFieldOfView = new Vector( -ratio, 0 );
			double angleRadians = MathHelper.DegreesToRadians( RotationTransform.Angle );
			VectorLine planeLine = new VectorLine( leftFieldOfView, new Vector( 0, Math.Tan( angleRadians ) * -ratio ) );
			double viewPercentage = mouseXPosition / Container2D.ActualWidth;
			double viewWidth = 2 * ratio;
			VectorLine viewRay = new VectorLine( new Vector( 0, 1 ), new Vector( -ratio + (viewWidth * viewPercentage), 0 ) );
			Vector viewIntersection = planeLine.Intersection( viewRay );

			// Find the percentage of the focused point on the time line.
			VectorLine rightFieldOfViewLine = new VectorLine( new Vector( 0, 1 ), new Vector( ratio, 0 ) );
			Vector rightIntersection = planeLine.Intersection( rightFieldOfViewLine );
			double planePercentage = leftFieldOfView.DistanceTo( viewIntersection ) / leftFieldOfView.DistanceTo( rightIntersection );

			long focusTicks = ticksInterval.GetValueAt( planePercentage );
			return new DateTime( focusTicks );
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

			DependencyProperty animatedProperty = TimeLine.GetDependencyProperty( TimeLineControl.Properties.VisibleInterval );
			_dragAnimation.Completed -= DragAnimationCompleted;
			TimeLine.VisibleInterval = TimeLine.VisibleInterval;	// Required to copy latest animated value to local value.
			TimeLine.BeginAnimation( animatedProperty, null );
			_dragAnimation = null;			
		}

		void OnMouseWheel( object sender, MouseWheelEventArgs e )
		{
			StopDragAnimation();

			// Calculate which time is focused.
			Interval<DateTime> visibleInterval = TimeLine.VisibleInterval;
			Interval<long> ticksInterval = ToTicksInterval( visibleInterval );
			var focusedTime = GetFocusedTime( visibleInterval, Mouse.GetPosition( this ).X );

			// Zoom the currently visible interval in/out.
			double zoom = 1.0 - ( -e.Delta * ZoomPercentage );
			double focusPercentage = ticksInterval.GetPercentageFor( focusedTime.Ticks );
			ticksInterval.Scale( zoom, focusPercentage );
			TimeLine.VisibleInterval = ToTimeInterval( ticksInterval );
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

		static Interval<long> ToTicksInterval( Interval<DateTime> interval )
		{
			return new Interval<long>( interval.Start.Ticks, interval.End.Ticks );
		}

		static Interval<DateTime> ToTimeInterval( Interval<long> interval )
		{
			long minTicks = DateTime.MinValue.Ticks;
			long maxTicks = DateTime.MaxValue.Ticks;

			return new Interval<DateTime>(
				new DateTime( interval.Start < minTicks ? minTicks : interval.Start ),
				new DateTime( interval.End > maxTicks ? maxTicks : interval.End ) );
		}

		double _timeLinePosition;
		void OnTimeLineDragEnter( object sender, DragEventArgs e )
		{
			IsTimeLineDraggedOver = true;
			_timeLinePosition = _timeIndicator.TranslatePoint( new Point( 0, 0 ), TimeLineContainer ).X;
		}

		void OnTimeLineDragLeave( object sender, DragEventArgs e )
		{
			IsTimeLineDraggedOver = false;
		}

		readonly TimeGate _throttleDragEvents = new TimeGate( TimeSpan.FromMilliseconds( 15 ), true );
		void OnTimeLineDragOver( object sender, DragEventArgs e )
		{
			// TODO: GetPosition on the 3D viewport seems to be so expensive that it locks up the rendering thread.
			//       Throttling the events somehwat resolves this, but is there a cleaner solution?
			if ( !_throttleDragEvents.TryEnter() )
			{
				return;
			}

			UpdateFocusedTime( e.GetPosition( this ).X );

			// Update cursor.
			// TODO: The reverse calculation of GetFocusedTime might speed up things.
			Point mouse = e.GetPosition( TimeLineContainer );
			double x = mouse.X;
			double y = mouse.Y;
			if ( x <= _timeLinePosition )
			{
				x -= DragDropCursor.ActualWidth;
			}
			y -= DragDropCursor.ActualHeight / 2;
			DragDropCursorPosition.SetValue( Canvas.LeftProperty, x );
			DragDropCursorPosition.SetValue( Canvas.TopProperty, y );
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

		void OnTimeLineDragDropped( object sender, DragEventArgs e )
		{
			var draggedTask = (ActivityViewModel)e.Data.GetData( typeof( ActivityViewModel ) );
			var overview = (ActivityOverviewViewModel)DataContext;

			if ( draggedTask != null && overview != null )
			{
				UpdateFocusedTime( e.GetPosition( this ).X );
				UpdateOffsetPercentage( e.GetPosition( TimeLineContainer ).Y );
				overview.TaskDropped( draggedTask );
			}

			IsTimeLineDraggedOver = false;
		}

		void OnContextMenuOpening( object sender, ContextMenuEventArgs e )
		{
			UpdateFocusedTime( Mouse.GetPosition( this ).X );
			UpdateOffsetPercentage( Mouse.GetPosition( TimeLineContainer ).Y );
		}
	}
}
