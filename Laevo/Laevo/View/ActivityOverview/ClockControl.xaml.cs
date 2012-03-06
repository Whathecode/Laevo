using System;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;


namespace Laevo.View.ActivityOverview
{
	/// <summary>
	///   Interaction logic for ClockControl.xaml
	///   TODO: Make format strings for date and time bindable?
	///         While attempting this I ran into problems you can't bind to properties of converters.
	///         A possible solution: http://www.codeproject.com/Articles/18678/Attaching-a-Virtual-Branch-to-the-Logical-Tree-in
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	public partial class ClockControl
	{
		[Flags]
		public enum Properties
		{
			Time
		}

		/// <summary>
		///   The currently visible interval.
		/// </summary>
		[DependencyProperty( Properties.Time )]
		public DateTime Time { get; set; }


		public ClockControl()
		{
			InitializeComponent();
		}
	}
}
