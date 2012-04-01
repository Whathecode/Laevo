using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Laevo.ViewModel.Activity.Binding;
using Microsoft.WindowsAPICodePack.Shell;
using VirtualDesktopManager;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.Activity
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]	
	class ActivityViewModel
	{
		readonly static object StaticLock = new object();

		const string IconResourceLocation = "view/activity/icons";
		public static List<BitmapImage> PresetIcons = new List<BitmapImage>();

		public static readonly List<Color> PresetColors = new List<Color>
		{
			Color.FromRgb( 86, 124, 212 ),
			Color.FromRgb( 88, 160, 2 ),
			Color.FromRgb( 193, 217, 197 )
		};
		public static readonly Color DefaultColor = PresetColors[ 0 ];
		public static BitmapImage DefaultIcon;
		public static BitmapImage HomeIcon;

		/// <summary>
		///   Path of the folder which contains the file libraries.
		/// </summary>
		const string LibraryName = "Activity Context";

		/// <summary>
		///   The extension of microsoft libraries.
		/// </summary>
		const string LibraryExtension = "library-ms";


		public delegate void ActivityEventHandler( ActivityViewModel viewModel );


		/// <summary>
		///   Event which is triggered when an activity is opened.
		/// </summary>
		public event ActivityEventHandler OpenedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is selected.
		/// </summary>
		public event ActivityEventHandler SelectedActivityEvent;

		readonly Model.Activity _activity;
		readonly DesktopManager _desktopManager;
		readonly VirtualDesktop _virtualDesktop;


		/// <summary>
		///   The time when the activity started.
		/// </summary>
		[NotifyProperty( Binding.Properties.Occurance )]
		public DateTime Occurance { get; set; }

		/// <summary>
		///   The entire timespan during which the activity has been open, regardless of whether it was closed in between.
		/// </summary>
		[NotifyProperty( Binding.Properties.TimeSpan )]
		public TimeSpan TimeSpan { get; set; }

		/// <summary>
		///   An icon representing the activity.
		/// </summary>
		[NotifyProperty( Binding.Properties.Icon )]
		public ImageSource Icon { get; set; }

		/// <summary>
		///   The background color for the activity representation. This color is used as the main color to construct a gradient.
		/// </summary>
		[NotifyProperty( Binding.Properties.Color )]
		public Color Color { get; set; }

		/// <summary>
		///   Text label which names the activity.
		/// </summary>
		[NotifyProperty( Binding.Properties.Label )]
		public string Label { get; set; }

		/// <summary>
		///   The percentage of the available height the activity box occupies.
		/// </summary>
		[NotifyProperty( Binding.Properties.HeightPercentage )]
		public double HeightPercentage { get; set; }

		/// <summary>
		///   The offset, as a percentage of the total available height, where to position the activity box, from the bottom.
		/// </summary>
		[NotifyProperty( Binding.Properties.OffsetPercentage )]
		public double OffsetPercentage { get; set; }


		static ActivityViewModel()
		{
			// Load icons.			
			var assembly = Assembly.GetExecutingAssembly();
			var resourcesName = assembly.GetName().Name + ".g";
			var manager = new ResourceManager( resourcesName, assembly );
			var resourceSet = manager.GetResourceSet( CultureInfo.CurrentUICulture, true, true );
			PresetIcons = resourceSet
				.OfType<DictionaryEntry>()
				.Where( r => r.Key.ToString().StartsWith( IconResourceLocation ) )
				.Select( r => new BitmapImage( new Uri( @"pack://application:,,/" + r.Key.ToString(), UriKind.Absolute ) ) )
				.ToList();

			DefaultIcon = PresetIcons.First( b => b.UriSource.AbsolutePath.Contains( "stats.png" ) );
			HomeIcon = PresetIcons.First( b => b.UriSource.AbsolutePath.Contains( "home.png" ) );			
		}

		public ActivityViewModel( Model.Activity activity, DesktopManager desktopManager )
			: this( activity, desktopManager, desktopManager.CreateEmptyDesktop() ) { }

		public ActivityViewModel( Model.Activity activity, DesktopManager desktopManager, VirtualDesktop virtualDesktop )
		{
			_activity = activity;
			Occurance = _activity.DateCreated;

			_desktopManager = desktopManager;
			_virtualDesktop = virtualDesktop;

			InitializeLibrary();

			Label = activity.Name;
			Icon = DefaultIcon;
			Color = DefaultColor;
			HeightPercentage = 0.2;
			OffsetPercentage = 1;
		}		


		[CommandExecute( Commands.OpenActivity )]
		public void OpenActivity()
		{
			_activity.Open();
			_desktopManager.SwitchToDesktop( _virtualDesktop );

			InitializeLibrary();

			OpenedActivityEvent( this );
		}

		[CommandExecute( Commands.OpenActivityLibrary )]
		public void OpenActivityLibrary()
		{
			string folderName = Path.Combine( ShellLibrary.LibrariesKnownFolder.Path, LibraryName );
			Process.Start( "explorer.exe", Path.ChangeExtension( folderName, LibraryExtension ) );
		}

		[CommandExecute( Commands.SelectActivity )]
		public void SelectActivity()
		{
			SelectedActivityEvent( this );
		}

		/// <summary>
		///   Initialize the library which contains all the context files.
		/// </summary>
		void InitializeLibrary()
		{
			// Initialize on a separate thread so the UI doesn't lock.		
			var dataPaths = _activity.DataPaths.Select( p => p.AbsolutePath ).ToArray();
			var initializeShellLibrary = new Thread( () =>
			{
				lock ( StaticLock )
				{
					var activityContext = new ShellLibrary( LibraryName, true );
					// TODO: Handle DirectoryNotFoundException when the folder no longer exists.
					Array.ForEach( dataPaths, activityContext.Add );
					activityContext.Close();
				}
			} );
			initializeShellLibrary.Start();
		}

		public void Update()
		{
			Occurance = _activity.DateCreated;
			if ( _activity.OpenIntervals.Count > 0 )
			{
				TimeSpan = _activity.OpenIntervals.Last().End - _activity.OpenIntervals.First().Start;
			}
		}
	}
}
