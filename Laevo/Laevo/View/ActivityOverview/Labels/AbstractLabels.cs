using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;


namespace Laevo.View.ActivityOverview.Labels
{
	abstract class AbstractLabels<T> : ILabels
		where T : FrameworkElement
	{
		readonly TimeSpan _extendVisibleRange;

		public ObservableCollection<FrameworkElement> Labels { get; private set; }

		protected readonly TimeLineControl TimeLine;
		protected readonly List<T> VisibleLabels = new List<T>();
		protected readonly Stack<T> AvailableLabels = new Stack<T>();

		Interval<DateTime> _currentVisibleInterval;
		Interval<DateTime> _extendedVisibleInterval;
		protected Interval<DateTime> ExtendedVisibleRange
		{
			get
			{
				if ( TimeLine.VisibleInterval != _currentVisibleInterval )
				{
					_extendedVisibleInterval = new Interval<DateTime>(
						TimeLine.VisibleInterval.Start.SafeSubtract( _extendVisibleRange ),
						TimeLine.VisibleInterval.End.SafeAdd( _extendVisibleRange ) );
					_currentVisibleInterval = TimeLine.VisibleInterval;
				}

				return _extendedVisibleInterval;
			}
		}


		protected AbstractLabels( TimeLineControl timeLine, TimeSpan extendVisibleRange )
		{
			TimeLine = timeLine;
			_extendVisibleRange = extendVisibleRange;
			Labels = new ObservableCollection<FrameworkElement>();
		}


		public void UpdatePositions()
		{
			if ( ShouldShowLabels() )
			{
				Interval<DateTime> visibleRange = TimeLine.VisibleInterval;
				List<DateTime> toPosition = GetPositions( visibleRange ).ToList();

				// Free up labels which aren't visible anymore, and determine which still need to be placed.
				var toRemove = new List<T>();
				foreach ( var visible in VisibleLabels )
				{
					var occurance = (DateTime)visible.GetValue( TimeLineControl.OccuranceProperty );
					if ( IsVisible( visible, occurance ) )
					{
						UpdateLabel( visible, occurance );
						toPosition.Remove( occurance );
					}
					else
					{
						AvailableLabels.Push( visible );
						visible.Visibility = Visibility.Hidden;
						toRemove.Add( visible );
					}
				}
				toRemove.ForEach( r => VisibleLabels.Remove( r ) );

				// Position all remaining labels.				
				foreach ( var date in toPosition.Where( ExtendedVisibleRange.LiesInInterval ) )
				{
					// Create a new label when needed.
					if ( AvailableLabels.Count == 0 )
					{
						var label = CreateNewLabel();
						AvailableLabels.Push( label );
						Labels.Add( label );
					}

					T toPlace = AvailableLabels.Pop();
					toPlace.SetValue( TimeLineControl.OccuranceProperty, date );
					toPlace.Visibility = Visibility.Visible;
					UpdateLabel( toPlace, date );
					VisibleLabels.Add( toPlace );
				}
			}
			else
			{
				// Not enough space in between labels, zoomed out too much.				
				VisibleLabels.ForEach( v => AvailableLabels.Push( v ) );
				VisibleLabels.Clear();
				AvailableLabels.ForEach( v => v.Visibility = Visibility.Hidden );				
			}
		}

		/// <summary>
		///   Returns all the visible positions within a certain interval.
		/// </summary>
		protected abstract IEnumerable<DateTime> GetPositions( Interval<DateTime> interval );

		/// <summary>
		///   Determines whether or not the labels should be shown.
		/// </summary>
		protected abstract bool ShouldShowLabels();

		protected abstract bool IsVisible( T label, DateTime occurance );

		protected abstract T CreateNewLabel();

		protected abstract void UpdateLabel( T label, DateTime occurance );
	}
}
