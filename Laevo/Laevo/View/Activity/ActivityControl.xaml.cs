using System;
using System.Collections.Generic;
using System.Windows.Media;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;


namespace Laevo.View.Activity
{
	/// <summary>
	/// Interaction logic for ActivityControl.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	public partial class ActivityControl
	{
		public static readonly List<Color> PresetColors = new List<Color>
		{
			Color.FromRgb( 86, 124, 212 ),
			Color.FromRgb( 88, 160, 2 ),
			Color.FromRgb( 193, 217, 197 )
		};


		[Flags]
		public enum Properties
		{
			Color,
			Label,
			ActivityHeight
		}


		/// <summary>
		///   The background color for the activity representation. This color is used as the main color to construct a gradient.
		/// </summary>
		[DependencyProperty( Properties.Color )]
		public Color Color { get; set; }

		/// <summary>
		///   Text label which names the activity.
		/// </summary>
		[DependencyProperty( Properties.Label )]
		public string Label { get; set; }

		/// <summary>
		///   The height of the box which represents the activity.
		/// </summary>
		[DependencyProperty( Properties.ActivityHeight )]
		public double ActivityHeight { get; set; }


		public ActivityControl()
		{
			InitializeComponent();
		}
	}
}
