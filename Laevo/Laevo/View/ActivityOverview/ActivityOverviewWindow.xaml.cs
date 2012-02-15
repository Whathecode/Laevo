using System;
using System.Windows.Controls;
using Whathecode.System.Arithmetic.Range;


namespace Laevo.View.ActivityOverview
{
	/// <summary>
	/// Interaction logic for ActivityOverviewWindow.xaml
	/// </summary>
	public partial class ActivityOverviewWindow
	{
		public ActivityOverviewWindow()
		{
			InitializeComponent();

			DateTime now = DateTime.Now;
			TimeLine.VisibleInterval = new Interval<DateTime>( now, now + TimeSpan.FromDays( 3 ) );
			var day1 = new Button { Content = "Day 1", Width = 100, Height = 100 };
			day1.SetValue( TimeLineControl.OccuranceProperty, now + TimeSpan.FromDays( 1 ) );
			var day2 = new Button { Content = "Day 2", Width = 100, Height = 100 };
			day2.SetValue( TimeLineControl.OccuranceProperty, now + TimeSpan.FromDays( 2 ) );
			TimeLine.Children.Add( day1 );
			TimeLine.Children.Add( day2 );
		}
	}
}
