using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using System.Linq;


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
		public ObservableCollection<UIElement> Children { get; private set; }


		#region Attached properties

		/// <summary>
		///   Identifies the Occurance property which indicates where the element should be positioned on the time line.
		/// </summary>
		public static readonly DependencyProperty OccuranceProperty = DependencyProperty.RegisterAttached( "Occurance", typeof( DateTime ), typeof( TimeLineControl ) );
		public static DateTime GetOccurance( UIElement element )
		{
			return (DateTime)element.GetValue( OccuranceProperty );
		}
		public static void SetOccurance( UIElement element, DateTime value )
		{
			element.SetValue( OccuranceProperty, value );
		}

		/// <summary>
		///   Identifies the Offset property which indicates the vertical offset from the time line.
		/// </summary>
		public static readonly DependencyProperty OffsetProperty = DependencyProperty.RegisterAttached( "Offset", typeof( double ), typeof( TimeLineControl ) );
		public static double GetOffset( UIElement element )
		{
			return (double)element.GetValue( OffsetProperty );
		}
		public static void SetOffset( UIElement element, double value )
		{
			element.SetValue( OffsetProperty, value );
		}

		#endregion // Attached properties


		public TimeLineControl()
		{
			InitializeComponent();

			Children = new ObservableCollection<UIElement>();
			Children.CollectionChanged += OnChildrenChanged;
		}


		void OnChildrenChanged( object sender, NotifyCollectionChangedEventArgs eventArgs )
		{
			switch ( eventArgs.Action )
			{
				case NotifyCollectionChangedAction.Add:
					// TODO: Bind to current viewport.
					// TODO: Bind Canvas.Bottom to Offset.
					break;
			}
		}
	}
}
