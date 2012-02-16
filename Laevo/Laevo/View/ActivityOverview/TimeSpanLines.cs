using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;


namespace Laevo.View.ActivityOverview
{
	/// <summary>
	///   Manages a set of labels indicating time intervals.
	/// </summary>
	/// <author>Steven Jeuris</author>
	class TimeSpanLabels
	{
		TimeSpan _interval;
		readonly List<FrameworkElement> _labels = new List<FrameworkElement>();

		public ReadOnlyCollection<FrameworkElement> Labels
		{
			get { return _labels.AsReadOnly(); }
		}


		public TimeSpanLabels( TimeSpan interval )
		{
			_interval = interval;

			// TODO: Remove temporary code.
			DateTime now = DateTime.Now;
			DateTime current = now - TimeSpan.FromHours( 2 );
			while ( current < now + TimeSpan.FromHours( 10 ) )
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
				line.SetValue( TimeLineControl.OccuranceProperty, current );
				_labels.Add( line );

				current += TimeSpan.FromMinutes( 5.0 );
			}
		}
	}
}
