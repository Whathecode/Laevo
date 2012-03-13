using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;
using Color = System.Windows.Media.Color;


namespace Laevo.View.Activity
{
	/// <summary>
	/// Interaction logic for ActivityControl.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	public partial class ActivityControl
	{
		const string IconResourceLocation = "view/activity/icons";
		public static List<BitmapImage> PresetIcons = new List<BitmapImage>();

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
			Icon,
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
		///   An icon representing the activity.
		/// </summary>
		[DependencyProperty( Properties.Icon )]
		public ImageSource Icon { get; set; }

		/// <summary>
		///   The height of the box which represents the activity.
		/// </summary>
		[DependencyProperty( Properties.ActivityHeight )]
		public double ActivityHeight { get; set; }


		static ActivityControl()
		{
			// Load icons.			
			var assembly = Assembly.GetExecutingAssembly();
			var resourcesName = assembly.GetName().Name + ".g";
			var manager = new ResourceManager( resourcesName, assembly );
			var resourceSet = manager.GetResourceSet( CultureInfo.CurrentUICulture, true, true );
			PresetIcons = resourceSet
				.OfType<DictionaryEntry>()
				.Where( r => r.Key.ToString().StartsWith( IconResourceLocation ) && !r.Key.ToString().EndsWith( ".baml" ) )
				.Select( r => new BitmapImage( new Uri( @"pack://application:,,/" + r.Key.ToString(), UriKind.Absolute ) ) )
				.ToList();
		}

		public ActivityControl()
		{
			InitializeComponent();
		}
	}
}
