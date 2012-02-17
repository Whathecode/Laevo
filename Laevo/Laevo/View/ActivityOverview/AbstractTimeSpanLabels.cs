using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.Extensions;


namespace Laevo.View.ActivityOverview
{
	/// <summary>
	///   Manages a set of labels indicating time intervals.
	/// </summary>
	/// <author>Steven Jeuris</author>
	abstract class AbstractTimeSpanLabels
	{
		const double MinimumSpaceBetweenLabels = 20.0;

		readonly List<FrameworkElement> _visibleLabels = new List<FrameworkElement>();
		readonly Stack<FrameworkElement> _availableLabels = new Stack<FrameworkElement>();
		public ObservableCollection<FrameworkElement> Labels { get; private set; }


		protected AbstractTimeSpanLabels()
		{
			Labels = new ObservableCollection<FrameworkElement>();		
		}


		public void UpdatePositions( Interval<DateTime> visibleRange, double width )
		{
			int maximumLabels = (int)Math.Ceiling( width / MinimumSpaceBetweenLabels );
			long visibleTicks = (visibleRange.End - visibleRange.Start).Ticks;

			if ( visibleTicks / GetMinimumTimeSpan().Ticks < maximumLabels )
			{
				// Enough space between each position is guaranteed.
				List<DateTime> toPosition = GetPositions( visibleRange ).ToList();

				// Free up labels which aren't visible anymore, and determine which still need to be placed.
				var toRemove = new List<FrameworkElement>();
				foreach ( var visible in _visibleLabels )
				{
					var occurance = (DateTime)visible.GetValue( TimeLineControl.OccuranceProperty );
					if ( !visibleRange.LiesInInterval( occurance ) )
					{
						_availableLabels.Push( visible );
						toRemove.Add( visible );
					}
					else
					{
						toPosition.Remove( occurance );						
					}
				}
				toRemove.ForEach( r => _visibleLabels.Remove( r ) );

				// Position all remaining labels.
				foreach ( var date in toPosition )
				{
					// Create a new label when needed.
					if ( _availableLabels.Count == 0 )
					{
						var line = new Line
						{
							X1 = 0,
							Y1 = 0,
							X2 = 0,
							Y2 = 500,
							Stroke = Brushes.White,
							StrokeThickness = 1
						};							
						_availableLabels.Push( line );
						Labels.Add( line );
					}

					FrameworkElement toPlace = _availableLabels.Pop();				
					toPlace.SetValue( TimeLineControl.OccuranceProperty, date );
					toPlace.Visibility = Visibility.Visible;
					_visibleLabels.Add( toPlace );
				}
			}
			else
			{
				// Not enough space in between labels, zoomed out too much.				
				_visibleLabels.ForEach( v => _availableLabels.Push( v ) );
				_visibleLabels.Clear();
				_availableLabels.ForEach( v => v.Visibility = Visibility.Hidden );
			}
		}

		/// <summary>
		///   Returns all the visible positions within a certain interval.
		/// </summary>
		protected abstract IEnumerable<DateTime> GetPositions( Interval<DateTime> interval );

		/// <summary>
		///   Gets the minimum time span between each label.
		/// </summary>
		protected abstract TimeSpan GetMinimumTimeSpan();
	}
}
