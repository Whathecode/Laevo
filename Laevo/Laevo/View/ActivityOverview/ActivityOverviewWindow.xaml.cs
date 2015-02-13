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
			MoveTimeLine = 1,
			IsSchedulingActivity
		}


		public const double TopOffset = 105;
		public const double BottomOffset = 45;

		const double ZoomPercentage = 0.001;
		const double DragMomentum = 0.0000001;

		readonly List<UnitLabels> _unitLabels = new List<UnitLabels>();
		readonly List<ILabels> _labels = new List<ILabels>();
		readonly Dictionary<WorkIntervalViewModel, WorkIntervalControl> _activityWorkIntervals = new Dictionary<WorkIntervalViewModel, WorkIntervalControl>();

		[DependencyProperty( Properties.MoveTimeLine )]
		public ICommand MoveTimeLineCommand { get; private set; }

		bool _isDragOverActivity;

		[DependencyProperty( Properties.IsSchedulingActivity )]
		public bool IsSchedulingActivity { get; private set; }

		readonly TimeIndicator _timeIndicator;


		public ActivityOverviewWindow()
		{
			InitializeComponent();

			MoveTimeLineCommand = new DelegateCommand<MouseBehavior.MouseDragCommandArgs>( MoveTimeLine );

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
				foreach ( var activityViewModel in oldViewModel.Activities )
				{
					activityViewModel.WorkIntervals.CollectionChanged -= WorkIntervalsChanged;
				}
			}

			var overviewViewModel = e.NewValue as ActivityOverviewViewModel;
			if ( overviewViewModel == null )
			{
				return;
			}
			foreach ( var activityViewModel in overviewViewModel.Activities )
			{
				activityViewModel.WorkIntervals.CollectionChanged += WorkIntervalsChanged;
				activityViewModel.WorkIntervals.ForEach( NewActivityWorkInterval );
			}
			overviewViewModel.Activities.CollectionChanged += ActivitiesChanged;
		}

		void ActivitiesChanged( object sender, NotifyCollectionChangedEventArgs e )
		{
			// Remove old items.
			if ( e.OldItems != null )
			{
				foreach ( var activity in e.OldItems.Cast<ActivityViewModel>() )
				{
					activity.WorkIntervals.CollectionChanged -= WorkIntervalsChanged;
					activity.WorkIntervals.ForEach( DeleteActivityWorkInterval );
				}
			}

			// Add new items.
			if ( e.NewItems != null )
			{
				foreach ( var activity in e.NewItems.Cast<ActivityViewModel>() )
				{
					activity.WorkIntervals.CollectionChanged += WorkIntervalsChanged;
				}
			}
		}

		void DeleteActivityWorkInterval( WorkIntervalViewModel viewModel )
		{
			WorkIntervalControl control = _activityWorkIntervals[ viewModel ];
			control.DragEnter -= OnActivityDragEnter;
			control.DragLeave -= OnActivityDragLeave;
			TimeLine.Children.Remove( control );
			_activityWorkIntervals.Remove( viewModel );
		}

		void WorkIntervalsChanged( object sender, NotifyCollectionChangedEventArgs e )
		{
			// Remove old items.
			if ( e.OldItems != null )
			{
				e.OldItems.Cast<WorkIntervalViewModel>().ForEach( DeleteActivityWorkInterval );
			}

			// Add new items.
			if ( e.NewItems != null )
			{
				e.NewItems.Cast<WorkIntervalViewModel>().ForEach( NewActivityWorkInterval );
			}
		}

		void NewActivityWorkInterval( WorkIntervalViewModel viewModel )
		{
			var control = new WorkIntervalControl
			{
				DataContext = viewModel,
				HorizontalAlignment = HorizontalAlignment.Left,
			};
			control.DragEnter += OnActivityDragEnter;
			control.DragLeave += OnActivityDragLeave;

			_activityWorkIntervals.Add( viewModel, control );
			TimeLine.Children.Add( control );
		}

		void OnActivityDragEnter( object sender, DragEventArgs e )
		{
			_isDragOverActivity = true;
		}

		void OnActivityDragLeave( object sender, DragEventArgs e )
		{
			_isDragOverActivity = false;
		}

		TimeInterval _startDrag;
		DateTime _startDragFocus;
		VisibleIntervalAnimation _dragAnimation;

		void MoveTimeLine( MouseBehavior.MouseDragCommandArgs info )
		{
			double mouseX = Mouse.GetPosition( this ).X;

			// Stop current time line animation.
			DependencyProperty visibleIntervalProperty = TimeLine.GetDependencyProperty( TimeLineControl.Properties.VisibleInterval );
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
					ConstantDeceleration = velocity * DragMomentum,
					TimeLine = TimeLine
				};
				_dragAnimation.Completed += DragAnimationCompleted;
				TimeLine.BeginAnimation( visibleIntervalProperty, _dragAnimation );
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

		DateTime GetFocusedTime( TimeInterval interval, double mouseXPosition )
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

			DependencyProperty animatedProperty = TimeLine.GetDependencyProperty( TimeLineControl.Properties.VisibleInterval );
			_dragAnimation.Completed -= DragAnimationCompleted;
			TimeLine.VisibleInterval = TimeLine.VisibleInterval; // Required to copy latest animated value to local value.
			TimeLine.BeginAnimation( animatedProperty, null );
			_dragAnimation = null;
		}

		void OnMouseWheel( object sender, MouseWheelEventArgs e )
		{
			StopDragAnimation();

			// Calculate which time is focused.
			TimeInterval visibleInterval = TimeLine.VisibleInterval;
			DateTime focusedTime = GetFocusedTime( visibleInterval, Mouse.GetPosition( this ).X );

			// Zoom the currently visible interval in/out.
			double zoom = 1.0 - ( e.Delta * ZoomPercentage );
			double focusPercentage = visibleInterval.GetPercentageFor( focusedTime );
			try
			{
				TimeLine.VisibleInterval = visibleInterval.Scale( zoom, focusPercentage );
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

		double _timeLinePosition;

		void OnTimeLineDragEnter( object sender, DragEventArgs e )
		{
			IsSchedulingActivity = !_isDragOverActivity;
			_timeLinePosition = _timeIndicator.TranslatePoint( new Point( 0, 0 ), TimeLineContainer ).X;

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