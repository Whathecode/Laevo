using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Laevo.View.Activity;
using Laevo.View.ActivityOverview.Labels;
using Laevo.ViewModel.ActivityOverview;
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

#if DEBUG
			WindowStyle = WindowStyle.None;
			WindowState = WindowState.Normal;
			Topmost = false;
			Width = 500;
			Height = 500;
#endif

			// Set focus so commands are triggered.
			TimeLine.Focus();

			// Set the time line's position around the current time when opening it.
			DateTime now = DateTime.Now;
			Activated += ( sender, args ) =>
			{
				var start = now - TimeSpan.FromHours( 1 );
				var end = now + TimeSpan.FromHours( 2 );
				TimeLine.VisibleInterval = new Interval<DateTime>( start, end );
			};

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
			_labels.Add( quarterUnits );
			var hourUnits = new UnitLabels( TimeLine, hours, "HH:mm", () => !quarterUnits.LabelsFitScreen() );
			_labels.Add( hourUnits );
			var sixHourUnits = new UnitLabels( TimeLine, everySixHours, "HH:mm", () => !hourUnits.LabelsFitScreen() );
			_labels.Add( sixHourUnits );
			var dayUnits = new UnitLabels( TimeLine, days, @"d\t\h", () => !sixHourUnits.LabelsFitScreen() );
			_labels.Add( dayUnits );
			var dayNameUnits = new UnitLabels( TimeLine, days, "dddd", () => !sixHourUnits.LabelsFitScreen(), 25, 18 );
			_labels.Add( dayNameUnits );
			var dayCompleteUnits = new UnitLabels( TimeLine, days, @"dddd d\t\h", () => sixHourUnits.LabelsFitScreen() && dayUnits.LabelsFitScreen(), 25, 18 );
			_labels.Add( dayCompleteUnits );
			var weekUnits = new UnitLabels( TimeLine, weeks, @"d\t\h", () => !dayUnits.LabelsFitScreen() );
			_labels.Add( weekUnits );
			var monthSmallUnits = new UnitLabels( TimeLine, months, "MMMM", () => !dayUnits.LabelsFitScreen() && weekUnits.LabelsFitScreen(), 25, 30 );
			_labels.Add( monthSmallUnits );
			var monthUnits = new UnitLabels( TimeLine, months, "MMMM", () => !weekUnits.LabelsFitScreen() );
			_labels.Add( monthUnits );

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

			// TODO: Remove test stuff.
			DataContextChanged += AddDemoActivities;
		}

		void AddDemoActivities( object sender, DependencyPropertyChangedEventArgs e )
		{
			var resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

			var vm = DataContext as ActivityOverviewViewModel;			
			var now = DateTime.Now;
			var test = new ActivityControl();
			test.DataContext = vm._desktops[ 0 ];
			test.SetValue( TimeLineControl.OccuranceProperty, now - TimeSpan.FromHours( 2 ) );
			test.SetValue( TimeLineControl.OffsetProperty, 100.0 );
			test.SetValue( TimeLineControl.TimeSpanProperty, TimeSpan.FromHours( 2 ) );
			test.HorizontalAlignment = HorizontalAlignment.Left;
			test.ActivityHeight = 50;
			test.Color = ActivityControl.PresetColors[ 0 ];
			test.Label = "Thesis";
			test.Icon = ActivityControl.PresetIcons[ 0 ];
			test.SetValue( Panel.ZIndexProperty, 100 );			
			TimeLine.Children.Add( test );
			var test2 = new ActivityControl();
			test2.DataContext = vm._desktops[ 1 ];
			test2.SetValue( TimeLineControl.OccuranceProperty, now - TimeSpan.FromHours( 1 ) );
			test2.SetValue( TimeLineControl.OffsetProperty, 200.0 );
			test2.SetValue( TimeLineControl.TimeSpanProperty, TimeSpan.FromHours( 1 ) );
			test2.HorizontalAlignment = HorizontalAlignment.Left;
			test2.ActivityHeight = 70;
			test2.Color = ActivityControl.PresetColors[ 1 ];
			test2.Label = "Programming";
			test2.Icon = ActivityControl.PresetIcons[ 1 ];
			test2.SetValue( Panel.ZIndexProperty, 100 );
			TimeLine.Children.Add( test2 );
			var test3 = new ActivityControl();
			test3.DataContext = vm._desktops[ 2 ];
			test3.SetValue( TimeLineControl.OccuranceProperty, now - TimeSpan.FromHours( 3 ) );
			test3.SetValue( TimeLineControl.OffsetProperty, 300.0 );
			test3.SetValue( TimeLineControl.TimeSpanProperty, TimeSpan.FromHours( 3 ) );
			test3.HorizontalAlignment = HorizontalAlignment.Left;
			test3.ActivityHeight = 30;
			test3.Color = ActivityControl.PresetColors[ 2 ];
			test3.Label = "Browsing";
			test3.Icon = ActivityControl.PresetIcons[ 2 ];
			test3.SetValue( Panel.ZIndexProperty, 100 );
			TimeLine.Children.Add( test3 );
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
