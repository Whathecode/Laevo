using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;


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
			Children
		}


		/// <summary>
		///   The currently visible interval.
		/// </summary>
		[DependencyProperty( Properties.VisibleInterval )]
		public Interval<DateTime> VisibleInterval { get; set; }

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

			Children = new ObservableCollection<FrameworkElement>();
			Children.CollectionChanged += OnChildrenChanged;
		}


		void OnChildrenChanged( object sender, NotifyCollectionChangedEventArgs eventArgs )
		{
			switch ( eventArgs.Action )
			{
				case NotifyCollectionChangedAction.Add:
					eventArgs.NewItems.Cast<FrameworkElement>().ForEach( e =>
					{
						// Position horizontally.
						MultiBinding positionBinding = new MultiBinding();
						Binding widthBinding = new Binding( "ActualWidth" ) { Source = this };
						positionBinding.Bindings.Add( widthBinding );
						Binding elementWidthBinding = new Binding( "ActualWidth" ) { Source = e };
						positionBinding.Bindings.Add( elementWidthBinding );
						Binding viewportBinding = new Binding( "VisibleInterval" ) { Source = this };
						positionBinding.Bindings.Add( viewportBinding );
						Binding occuranceBinding = new Binding { Path = new PropertyPath( OccuranceProperty ), Source = e };
						positionBinding.Bindings.Add( occuranceBinding );
						positionBinding.Converter = new TimeLinePositionConverter();
						e.SetBinding( Canvas.LeftProperty, positionBinding );

						// Position vertically.
						Binding bottomBinding = new Binding { Path = new PropertyPath( OffsetProperty ), Source = e };
						e.SetBinding( Canvas.BottomProperty, bottomBinding );
					} );
					break;
			}
		}
	}
}
