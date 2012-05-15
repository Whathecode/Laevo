﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes.Coercion;


namespace Laevo.View.ActivityOverview
{
	/// <summary>
	///   Interaction logic for TimeLineControl.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	public partial class TimeLineControl
	{
		[Flags]
		public enum Properties
		{
			VisibleInterval,
			InternalVisibleInterval,
			Minimum,
			Maximum,
			MinimumTimeSpan,
			MaximumTimeSpan,
			Children
		}


		public delegate void VisibleIntervalChangedEventHandler( Interval<DateTime> interval );
		/// <summary>
		///   Event which is triggered when the visible interval is changed.
		/// </summary>
		public event VisibleIntervalChangedEventHandler VisibleIntervalChangedEvent;

		/// <summary>
		///   The currently visible interval.
		/// </summary>
		[DependencyProperty( Properties.VisibleInterval )]
		[CoercionHandler( typeof( VisibleIntervalCoercion ) )]
		public Interval<DateTime> VisibleInterval { get; set; }

		[DependencyProperty( Properties.InternalVisibleInterval )]
		public Interval<long> InternalVisibleInterval { get; private set; }

		[DependencyProperty( Properties.Minimum )]
		public DateTime? Minimum { get; set; }

		[DependencyProperty( Properties.Maximum )]
		public DateTime? Maximum { get; set; }

		[DependencyProperty( Properties.MinimumTimeSpan )]
		public TimeSpan? MinimumTimeSpan { get; set; }

		[DependencyProperty( Properties.MaximumTimeSpan )]
		public TimeSpan? MaximumTimeSpan { get; set; }

		/// <summary>
		///   Collection of elements which are placed at a specific point, or timespan in time.
		/// </summary>
		[DependencyProperty( Properties.Children )]
		public ObservableCollection<FrameworkElement> Children { get; private set; }


		#region Attached properties

		/// <summary>
		///   Identifies the Occurance property which indicates where the element should be positioned on the time line.
		/// </summary>
		public static readonly DependencyProperty OccuranceProperty
			= DependencyProperty.RegisterAttached( "Occurance", typeof( DateTime ), typeof( TimeLineControl ) );
		public static DateTime GetOccurance( FrameworkElement element )
		{
			return (DateTime)element.GetValue( OccuranceProperty );
		}
		public static void SetOccurance( FrameworkElement element, DateTime value )
		{
			element.SetValue( OccuranceProperty, value );
		}

		/// <summary>
		///   Identifies the TimeSpan property which indicates how much time the element should occupy on the time line.
		/// </summary>
		public static readonly DependencyProperty TimeSpanProperty
			= DependencyProperty.RegisterAttached( "TimeSpan", typeof( TimeSpan ), typeof( TimeLineControl ) );
		public static TimeSpan GetTimeSpan( FrameworkElement element )
		{
			return (TimeSpan)element.GetValue( TimeSpanProperty );
		}
		public static void SetTimeSpan( FrameworkElement element, TimeSpan value )
		{
			element.SetValue( TimeSpanProperty, value );
		}

		/// <summary>
		///   Identifies the Offset property which indicates the vertical offset from the time line.
		/// </summary>
		public static readonly DependencyProperty OffsetProperty
			= DependencyProperty.RegisterAttached( "Offset", typeof( double ), typeof( TimeLineControl ) );

		public static double GetOffset( FrameworkElement element )
		{
			return (double)element.GetValue( OffsetProperty );
		}
		public static void SetOffset( FrameworkElement element, double value )
		{
			element.SetValue( OffsetProperty, value );
		}

		#endregion // Attached properties


		public TimeLineControl()
		{
			InitializeComponent();

			RenderTransform = new TranslateTransform();
			VisibleInterval = new Interval<DateTime>( DateTime.Today, DateTime.Today.SafeAdd( TimeSpan.FromDays( 1 ) ) );

			Children = new ObservableCollection<FrameworkElement>();
			Children.CollectionChanged += OnChildrenChanged;
		}

		public long GetVisibleTicks()
		{
			return (VisibleInterval.End - VisibleInterval.Start).Ticks;
		}

		/// <summary>
		///   Move the interval by a specified time span.
		/// </summary>
		/// <param name="timeSpan">The amount of time to move the interval.</param>
		/// <param name="moveForward">True when interval needs to be moved forward, false otherwise.</param>
		public void MoveInterval( TimeSpan timeSpan, bool moveForward = true )
		{
			long ticks = moveForward ? timeSpan.Ticks : -timeSpan.Ticks;
			Func<DateTime, DateTime> operation;
			if ( ticks > 0 )
			{
				operation = d => d.SafeAdd( TimeSpan.FromTicks( ticks ) );
			}
			else
			{
				operation = d => d.SafeSubtract( TimeSpan.FromTicks( ticks ) );
			}

			VisibleInterval = new Interval<DateTime>(
				operation( VisibleInterval.Start ),
				operation( VisibleInterval.End ) );
		}

		void OnChildrenChanged( object sender, NotifyCollectionChangedEventArgs eventArgs )
		{
			switch ( eventArgs.Action )
			{
				case NotifyCollectionChangedAction.Add:
					eventArgs.NewItems.Cast<FrameworkElement>().ForEach( e =>
					{
						// Position horizontally.
						var positionBinding = new MultiBinding { ConverterParameter = this };
						var timeLineWidth = new Binding( "ActualWidth" ) { Source = this };
						positionBinding.Bindings.Add( timeLineWidth );
						var elementWidth = new Binding( "ActualWidth" ) { Source = e };
						positionBinding.Bindings.Add( elementWidth );
						var viewport = new Binding( "InternalVisibleInterval" ) { Source = this };
						positionBinding.Bindings.Add( viewport );
						var occurance = new Binding { Path = new PropertyPath( OccuranceProperty ), Source = e };
						positionBinding.Bindings.Add( occurance );
						var alignment = new Binding( "HorizontalAlignment" ) { Source = e };
						positionBinding.Bindings.Add( alignment  );
						positionBinding.Converter = new ActivityPositionConverter();
						// TODO: Is the TranslateTransform faster?
						//e.SetBinding( Canvas.LeftProperty, positionBinding );
						var transform = new TranslateTransform( 0, 0 );						
						BindingOperations.SetBinding( transform, TranslateTransform.XProperty, positionBinding );
						e.RenderTransform = transform;

						// Position vertically.
						var bottom = new Binding
						{
							Path = new PropertyPath( OffsetProperty ),
							Source = e
						};
						e.SetBinding( Canvas.BottomProperty, bottom );

						// Resize width.
						var widthBinding = new MultiBinding();						
						widthBinding.Bindings.Add( timeLineWidth );
						widthBinding.Bindings.Add( viewport );
						var timeSpan = new Binding { Path = new PropertyPath( TimeSpanProperty ), Source = e };
						widthBinding.Bindings.Add( timeSpan );
						widthBinding.Converter = new ActivityWidthConverter();
						e.SetBinding( WidthProperty, widthBinding );
					} );
					break;

				case NotifyCollectionChangedAction.Remove:
					eventArgs.OldItems.Cast<FrameworkElement>().ForEach( BindingOperations.ClearAllBindings );
					break;
			}
		}

		[DependencyPropertyChanged( Properties.VisibleInterval )]
		static void OnVisibleIntervalChanged( DependencyObject o, DependencyPropertyChangedEventArgs e )
		{
			var control = (TimeLineControl)o;
			var newInterval = (Interval<DateTime>)e.NewValue;
			var newTicksInterval = new Interval<long>( newInterval.Start.Ticks, newInterval.End.Ticks );

			bool changeInternalInterval = true;
			if ( e.OldValue != null )
			{
				var oldInterval = (Interval<DateTime>)e.OldValue;
				var oldTicksInterval = new Interval<long>( oldInterval.Start.Ticks, oldInterval.End.Ticks );
				if ( oldTicksInterval.Size == newTicksInterval.Size )
				{
					changeInternalInterval = false;
				}
			}
			if ( changeInternalInterval )
			{
				control.InternalVisibleInterval = newTicksInterval;										
			}

			// Set required transform based on difference between the internal interval and the actual interval.
			var transform = (TranslateTransform)control.RenderTransform;
			long ticksDifference = control.InternalVisibleInterval.Start - control.VisibleInterval.Start.Ticks;
			transform.X = (double)ticksDifference / control.InternalVisibleInterval.Size * control.ActualWidth;			

			control.VisibleIntervalChangedEvent( control.VisibleInterval );
		}
	}
}
