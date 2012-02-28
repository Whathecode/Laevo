using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Laevo.View.ActivityOverview.Labels;
using Whathecode.System;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Collections.Generic;
using Whathecode.System.Extensions;
using Whathecode.System.Xaml.Behaviors;


namespace Laevo.View.ActivityOverview
{
	/// <summary>
	/// Interaction logic for ActivityOverviewWindow.xaml
	/// </summary>
	public partial class ActivityOverviewWindow
	{
		const double ZoomPercentage = 0.001;

		public static readonly RoutedCommand MouseDragged = new RoutedCommand( "MouseDragged", typeof( ActivityOverviewWindow ) );
		DateTime _focusedTime = DateTime.Now;

		readonly List<ILabels> _labels = new List<ILabels>();


		public ActivityOverviewWindow()
		{
			InitializeComponent();

			// Set focus so commands are triggered.
			TimeLine.Focus();

			// Set the time line's position around the current time when opening it.
			DateTime now = DateTime.Now;
			Activated += ( sender, args ) =>
			{
				var start = now - TimeSpan.FromHours( 2 );
				var end = now + TimeSpan.FromHours( 1 );
				TimeLine.VisibleInterval = new Interval<DateTime>( start, end );
			};

			// Create desired intervals to show.
			// TODO: This logic seems abstract enough to move to the model.
			var weeks = new RegularInterval(
				d => d.Round( DateTimePart.Day ) - TimeSpan.FromDays( (int)d.DayOfWeek == 0 ? 6 : (int)d.DayOfWeek - 1 ),
				TimeSpan.FromDays( 7 ),
				"MMMM d");
			var days = new RegularInterval( 1, DateTimePart.Day, @"d\t\h" );
			var everySixHours = new RegularInterval( 6, DateTimePart.Hour, @"H tt" );
			var hours = new RegularInterval( 1, DateTimePart.Hour, @"H:00" );
			var quarters = new RegularInterval( 15, DateTimePart.Minute, "HH:mm" );
			AbstractInterval[] intervals = { weeks, days, everySixHours, hours, quarters };

			// Create vertical interval lines.			
			var labelList = new TupleList<RegularInterval, Func<DateTime, bool>>
			{
				{ weeks, d => true },
				{ days, d => d.DayOfWeek != DayOfWeek.Monday },
				{ everySixHours, d => d.Hour.EqualsAny( 6, 12, 18 ) },
				{ hours, d => d.Hour != 0 && !d.Hour.EqualsAny( 6, 12, 18 ) },
				{ quarters, d => d.Minute != 0 }
			};
			labelList.ForEach( l => _labels.Add( new RegularIntervalLines( TimeLine, l.Item1, l.Item2 ) ) );

			// TODO: Add variant labels. (months/years)

			// Add header labels.
			_labels.Add( new HeaderLabels( TimeLine, intervals ) );
			_labels.Add( new BreadcrumbLabels( TimeLine, intervals ) );

			// Hook up all labels to listen to time line changes.
			_labels.Select( l => l.Labels ).ForEach( l => l.CollectionChanged += (s, e) =>
			{
				switch ( e.Action )
				{
					case NotifyCollectionChangedAction.Add:
						e.NewItems.Cast<FrameworkElement>().ForEach( i => TimeLine.Children.Add( i ) );
						break;
				}
			} );
			Action updatePositions = () =>  _labels.ForEach( l => l.UpdatePositions() );
			TimeLine.VisibleIntervalChangedEvent += i => updatePositions();
			var widthDescriptor = DependencyPropertyDescriptor.FromProperty( ActualWidthProperty, typeof( TimeLineControl ) );
			widthDescriptor.AddValueChanged( TimeLine, (s, e) => updatePositions() );			
		}


		Interval<long> _startDrag;
		void MoveTimeLine( object sender, ExecutedRoutedEventArgs e )
		{
			var info = (MouseBehavior.ClickDragInfo)e.Parameter;
			if ( info.State == MouseBehavior.ClickDragState.Start )
			{
				_startDrag = ToTicksInterval( TimeLine.VisibleInterval );
			}
			else
			{
				double offsetPercentage = info.Mouse.Position.Percentage.X - info.StartPosition.Percentage.X;
				var visibleTicks = ToTicksInterval( TimeLine.VisibleInterval ).Size;
				var ticksOffset = (long)(visibleTicks * offsetPercentage);
				var interval = (Interval<long>)_startDrag.Clone();
				interval.Move( -ticksOffset );
				TimeLine.VisibleInterval = ToTimeInterval( interval );
			}
		}	

		void OnMouseMoved( object sender, MouseEventArgs e )
		{
			double horizontalPercentage = e.GetPosition( this ).X / ActualWidth;
			long focusTicks = ToTicksInterval( TimeLine.VisibleInterval ).GetValueAt( horizontalPercentage );
			_focusedTime = new DateTime( focusTicks );
		}

		void OnMouseWheel( object sender, MouseWheelEventArgs e )
		{
			double zoom = 1.0 - (-e.Delta * ZoomPercentage);
			Interval<long> ticksInterval = ToTicksInterval( TimeLine.VisibleInterval );
			ticksInterval.Scale( zoom, ticksInterval.GetPercentageFor( _focusedTime.Ticks ) );
			TimeLine.VisibleInterval = ToTimeInterval( ticksInterval );
		}

		static Interval<long> ToTicksInterval( Interval<DateTime> interval )
		{
			return new Interval<long>( interval.Start.Ticks, interval.End.Ticks );
		}

		static Interval<DateTime> ToTimeInterval( Interval<long> interval )
		{
			return new Interval<DateTime>( new DateTime( interval.Start ), new DateTime( interval.End ) );
		}
	}
}
