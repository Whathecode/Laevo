using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Laevo.Model.AttentionShifts;
using Laevo.View.Activity;
using Laevo.ViewModel.ActivityOverview;
using Laevo.ViewModel.ActivityOverview.Binding;
using Microsoft.WindowsAPICodePack.Shell;
using VirtualDesktopManager;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;
using Commands = Laevo.ViewModel.Activity.Binding.Commands;


namespace Laevo.ViewModel.Activity
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	[DataContract]
	[KnownType( typeof( BitmapImage ) )]
	class ActivityViewModel : AbstractViewModel
	{		
		readonly ActivityOverviewViewModel _overview;

		const string IconResourceLocation = "view/activity/icons";
		public static List<BitmapImage> PresetIcons { get; private set; }

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

		/// <summary>
		///   Event which is triggered when starting to edit an activity.
		/// </summary>
		public event ActivityEventHandler ActivityEditStartedEvent;

		/// <summary>
		///   Event which is triggered when finished editing an activity.
		/// </summary>
		public event ActivityEventHandler ActivityEditFinishedEvent;

		readonly Model.Activity _activity;
		readonly DesktopManager _desktopManager;
		VirtualDesktop _virtualDesktop;


		/// <summary>
		///   The time when the activity was created.
		/// </summary>
		public DateTime DateCreated
		{
			get { return _activity.DateCreated; }
		}

		/// <summary>
		///   The time when the activity started.
		/// </summary>
		[NotifyProperty( Binding.Properties.Occurance )]
		public DateTime Occurance { get; private set; }

		/// <summary>
		///   The entire timespan during which the activity has been open, regardless of whether it was closed in between.
		/// </summary>
		[NotifyProperty( Binding.Properties.TimeSpan )]
		public TimeSpan TimeSpan { get; private set; }

		Interval<DateTime> _currentActiveTimeSpan;
		/// <summary>
		///   The timespans during which the activity was active. Multiple activities can be open, but only one can be active at a time.
		/// </summary>
		[NotifyProperty( Binding.Properties.ActiveTimeSpans )]
		public ObservableCollection<Interval<DateTime>> ActiveTimeSpans { get; private set; }

		/// <summary>
		///   An icon representing the activity.
		/// </summary>
		[NotifyProperty( Binding.Properties.Icon )]
		[DataMember]
		public ImageSource Icon { get; set; }

		/// <summary>
		///   The background color for the activity representation. This color is used as the main color to construct a gradient.
		/// </summary>
		[NotifyProperty( Binding.Properties.Color )]
		[DataMember]
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
		[DataMember]
		public double HeightPercentage { get; set; }

		/// <summary>
		///   The offset, as a percentage of the total available height, where to position the activity box, from the bottom.
		/// </summary>
		[NotifyProperty( Binding.Properties.OffsetPercentage )]
		[DataMember]
		public double OffsetPercentage { get; set; }

		[NotifyProperty( Binding.Properties.PossibleColors )]
		public ObservableCollection<Color> PossibleColors { get; set; }

		[NotifyProperty( Binding.Properties.PossibleIcons )]
		public ObservableCollection<BitmapImage> PossibleIcons { get; set; }


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

		public ActivityViewModel( ActivityOverviewViewModel overview, Model.Activity activity, DesktopManager desktopManager )
		{
			_overview = overview;
			_activity = activity;

			_desktopManager = desktopManager;
			_virtualDesktop = desktopManager.CreateEmptyDesktop();

			Label = activity.Name;
			Icon = DefaultIcon;
			Color = DefaultColor;
			HeightPercentage = 0.2;
			OffsetPercentage = 1;

			CommonInitialize();			
		}

		public ActivityViewModel(
			ActivityOverviewViewModel overview,
			Model.Activity activity,
			DesktopManager desktopManager,
			ActivityViewModel storedViewModel,
			IEnumerable<ActivityAttentionShift> activitySwitches )
		{
			_overview = overview;
			_activity = activity;

			_desktopManager = desktopManager;
			_virtualDesktop = desktopManager.CreateEmptyDesktop();

			Label = activity.Name;
			Icon = storedViewModel.Icon;
			Color = storedViewModel.Color;
			HeightPercentage = storedViewModel.HeightPercentage;
			OffsetPercentage = storedViewModel.OffsetPercentage;

			CommonInitialize();

			// Initiate attention history.
			Model.Activity lastActivity = null;
			ActivityAttentionShift lastShift = null;
			foreach ( var s in activitySwitches )
			{
				if ( s.Activity == activity )
				{					
					if ( _currentActiveTimeSpan != null && lastShift != null )
					{
						// Activity reopened. First close previous open interval.
						var closedInterval = activity.OpenIntervals.First( i => i.LiesInInterval( lastShift.Time ) );
						_currentActiveTimeSpan.ExpandTo( closedInterval.End );
					}

					// Activity opened.
					_currentActiveTimeSpan = new Interval<DateTime>( s.Time, s.Time );
					ActiveTimeSpans.Add( _currentActiveTimeSpan );						
				}
				else if ( _currentActiveTimeSpan != null )
				{
					// Switched from this activity, to other activity.
					_currentActiveTimeSpan.ExpandTo( s.Time );
					_currentActiveTimeSpan = null;					
				}
				lastShift = s;
				lastActivity = s.Activity;
			}
			if ( _currentActiveTimeSpan != null && lastActivity != null )
			{
				// Since the application shut down, the activity wasn't open afterwards.
				_currentActiveTimeSpan.ExpandTo( lastActivity.OpenIntervals.Last().End );
				_currentActiveTimeSpan = null;
			}
		}

		void CommonInitialize()
		{
			PossibleColors = new ObservableCollection<Color>( PresetColors );
			PossibleIcons = new ObservableCollection<BitmapImage>( PresetIcons );
			ActiveTimeSpans = new ObservableCollection<Interval<DateTime>>();			
		}


		static bool _firstActivity = true;
		/// <summary>
		///   Open the activity.
		///   When it is the first activity opened, the current open windows will merge with the stored ones.
		/// </summary>
		[CommandExecute( Commands.OpenActivity )]
		public void OpenActivity()
		{
			if ( this == _overview.CurrentActivityViewModel )
			{
				// Activity is already open.
				// The event is still necessary to indicate the user is no longer selecting an activity.
				OpenedActivityEvent( this );
				return;
			}

			// The first opened activity should include the currently open windows.
			if ( _firstActivity )
			{
				_virtualDesktop = _desktopManager.Merge( _virtualDesktop, _desktopManager.CurrentDesktop );
				_firstActivity = false;
			}

			// Activity also becomes active when it is opened.
			if ( ActiveTimeSpans == null )
			{
				ActiveTimeSpans = new ObservableCollection<Interval<DateTime>>();
			}
			DateTime now = DateTime.Now;
			_currentActiveTimeSpan = new Interval<DateTime>( now, now );
			ActiveTimeSpans.Add( _currentActiveTimeSpan );

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
			switch ( _overview.ActivityMode )
			{
				case Mode.Select:
					SelectedActivityEvent( this );
					break;
				case Mode.Open:
					OpenActivity();
					break;
			}			
		}

		[CommandExecute( Commands.EditActivity )]
		public void EditActivity()
		{
			ActivityEditStartedEvent( this );

			var popup = new EditActivityPopup
			{
				DataContext = this
			};
			popup.Closed += ( s, a ) => ActivityEditFinishedEvent( this );
			popup.Show();
		}

		[CommandExecute( Commands.ChangeColor )]
		public void ChangeColor( Color newColor )
		{
			Color = newColor;
		}

		[CommandExecute( Commands.ChangeIcon )]
		public void ChangeIcon( BitmapImage newIcon )
		{
			Icon = newIcon;
		}

		/// <summary>
		///   Initialize the library which contains all the context files.
		/// </summary>
		void InitializeLibrary()
		{
			// Information about Shell Libraries: http://msdn.microsoft.com/en-us/library/windows/desktop/dd758094(v=vs.85).aspx

			var dataPaths = _activity.DataPaths.Select( p => p.LocalPath ).ToArray();
			using ( var activityContext = new ShellLibrary( LibraryName, true ) )
			{
				// TODO: Handle DirectoryNotFoundException when the folder no longer exists.
				Array.ForEach( dataPaths, activityContext.Add );
			}
		}

		public void Update( DateTime now )
		{
			// Update the interval which indicates when the activity was open.
			if ( _activity.OpenIntervals.Count > 0 )
			{
				Occurance = _activity.OpenIntervals.First().Start;
				TimeSpan = _activity.OpenIntervals.Last().End - _activity.OpenIntervals.First().Start;
			}
			else
			{
				Occurance = _activity.DateCreated;
			}

			// Update the intervals which indicate when the activity was active.
			if ( _currentActiveTimeSpan != null )
			{
				_currentActiveTimeSpan.ExpandTo( now );
			}
		}

		/// <summary>
		///   Called by ActivityOverviewModel once the user has opened an activity other than this one.
		/// </summary>
		internal void Deactivated()
		{
			_activity.Deactivate();
			_currentActiveTimeSpan = null;
		}

		public override void Persist()
		{
			_activity.Name = Label;
		}

		protected override void FreeUnmanagedResources()
		{
			// Nothing to do.
		}
	}
}
