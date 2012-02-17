using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Whathecode.System.Arithmetic.Range;
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

		readonly List<AbstractTimeSpanLabels> _labels = new List<AbstractTimeSpanLabels>();


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

			// Add labels.
			_labels.Add( new RegularLabels( TimeSpan.FromHours( 1 ), Extensions.DateTimePart.Hour ) );
			_labels.Select( l => l.Labels ).ForEach( l => l.CollectionChanged += (s, e) =>
			{
				switch ( e.Action )
				{
					case NotifyCollectionChangedAction.Add:
						e.NewItems.Cast<FrameworkElement>().ForEach( i => TimeLine.Children.Add( i ) );
						break;
				}
			} );
			Action updatePositions = () => _labels.ForEach( l => l.UpdatePositions( TimeLine.VisibleInterval, TimeLine.ActualWidth ) );
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
