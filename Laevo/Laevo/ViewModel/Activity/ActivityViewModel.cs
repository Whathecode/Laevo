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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ABC.Windows.Desktop;
using Laevo.Model.AttentionShifts;
using Laevo.View.Activity;
using Laevo.ViewModel.ActivityOverview;
using Laevo.ViewModel.ActivityOverview.Binding;
using Microsoft.WindowsAPICodePack.Shell;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;
using Commands = Laevo.ViewModel.Activity.Binding.Commands;


namespace Laevo.ViewModel.Activity
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	[DataContract]
	[KnownType( typeof( BitmapImage ) )]
	[KnownType( typeof( StoredSession ) )]
	class ActivityViewModel : AbstractViewModel
	{
		readonly ActivityOverviewViewModel _overview;

		const string IconResourceLocation = "view/activity/icons";
		public static List<BitmapImage> PresetIcons { get; private set; }

		public static readonly List<Color> PresetColors = new List<Color>
		{			
			Color.FromRgb( 86, 124, 212 ),	// Blue
			Color.FromRgb( 121, 234, 255 ),	// Cyan
			Color.FromRgb( 88, 160, 2 ),	// Green
			Color.FromRgb( 227, 220, 0 ),	// Yellow
			Color.FromRgb( 212, 131, 0 ),	// Orange
			Color.FromRgb( 212, 50, 38 ),	// Red
			Color.FromRgb( 193, 75, 159 ),	// Purple
			Color.FromRgb( 193, 217, 197 ),	// Gray/White
			Color.FromRgb( 49, 54, 52 )		// Dark gray
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
		///   Event which is triggered at the start when an acitvity is being activated when it wasn't activated before.
		/// </summary>
		public event ActivityEventHandler ActivatingActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is activated.
		/// </summary>
		public event ActivityEventHandler ActivatedActivityEvent;

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

		/// <summary>
		///   Event which is triggered when activity is closed.
		/// </summary>
		public event ActivityEventHandler ActivityClosedEvent;

		internal readonly Model.Activity Activity;
		readonly VirtualDesktopManager _desktopManager;
		[DataMember]
		VirtualDesktop _virtualDesktop;


		/// <summary>
		///   The time when the activity was created.
		/// </summary>
		public DateTime DateCreated
		{
			get { return Activity.DateCreated; }
		}

		/// <summary>
		///   The time when the activity started or will start.
		/// </summary>
		[NotifyProperty( Binding.Properties.Occurance )]
		public DateTime Occurance { get; private set; }

		[NotifyPropertyChanged( Binding.Properties.Occurance )]
		public void OnOccuranceChanged( DateTime oldOccurance, DateTime newOccurance )
		{
			if ( IsPlannedActivity )
			{
				Activity.Plan( newOccurance, TimeSpan );
			}
		}

		[NotifyProperty( Binding.Properties.IsPlannedActivity )]
		public bool IsPlannedActivity { get; private set; }

		/// <summary>
		///   The entire timespan during which the activity has been open, regardless of whether it was closed in between.
		/// </summary>
		[NotifyProperty( Binding.Properties.TimeSpan )]
		public TimeSpan TimeSpan { get; private set; }

		[NotifyPropertyChanged( Binding.Properties.TimeSpan )]
		public void OnTimeSpanChanged( TimeSpan oldDuration, TimeSpan newDuration )
		{
			if ( IsPlannedActivity )
			{
				Activity.Plan( Occurance, newDuration );
			}
		}

		Interval<DateTime> _currentActiveTimeSpan;
		/// <summary>
		///   The timespans during which the activity was active. Multiple activities can be open, but only one can be active at a time.
		/// </summary>
		[NotifyProperty( Binding.Properties.ActiveTimeSpans )]
		public ObservableCollection<Interval<DateTime>> ActiveTimeSpans { get; private set; }

		/// <summary>
		///   Determines whether or not the active timespans should be shown.
		/// </summary>
		[NotifyProperty( Binding.Properties.ShowActiveTimeSpans )]
		public bool ShowActiveTimeSpans { get; set; }

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

		[NotifyPropertyChanged( Binding.Properties.Label )]
		public void OnLabelChanged( string oldLabel, string newLabel )
		{
			Activity.Name = newLabel;
			InitializeLibrary();
		}

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

		/// <summary>
		///   Determines whether or not the activity is currently active (working on it).
		/// </summary>
		[NotifyProperty( Binding.Properties.IsActive )]
		public bool IsActive { get; set; }

		/// <summary>
		///   Determines whether or not the activity is currently open, but not necessarily active (working on it).
		/// </summary>
		[NotifyProperty( Binding.Properties.IsOpen )]
		public bool IsOpen { get; set; }

		[NotifyProperty( Binding.Properties.HasOpenWindows )]
		public bool HasOpenWindows { get; private set; }

		[NotifyProperty( Binding.Properties.HasUnattendedInterruptions )]
		public bool HasUnattendedInterruptions { get; private set; }

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

			DefaultIcon = PresetIcons.First( b => b.UriSource.AbsolutePath.Contains( "laevo.png" ) );
			HomeIcon = PresetIcons.First( b => b.UriSource.AbsolutePath.Contains( "home.png" ) );
		}

		public ActivityViewModel( ActivityOverviewViewModel overview, Model.Activity activity, VirtualDesktopManager desktopManager )
			: this( overview, activity, desktopManager, desktopManager.CreateEmptyDesktop() )
		{
		}

		public ActivityViewModel( ActivityOverviewViewModel overview, Model.Activity activity, VirtualDesktopManager desktopManager, VirtualDesktop desktop )
		{
			_overview = overview;
			Activity = activity;

			_desktopManager = desktopManager;
			_virtualDesktop = desktop;

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
			VirtualDesktopManager desktopManager,
			ActivityViewModel storedViewModel,
			IEnumerable<ActivityAttentionShift> activitySwitches )
		{
			_overview = overview;
			Activity = activity;

			_desktopManager = desktopManager;
			_virtualDesktop = storedViewModel._virtualDesktop ?? desktopManager.CreateEmptyDesktop();

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
			Activity.ActivatedEvent += a => IsActive = true;
			Activity.DeactivatedEvent += a => IsActive = false;
			Activity.OpenedEvent += a => IsOpen = true;
			Activity.ClosedEvent += a => IsOpen = false;

			PossibleColors = new ObservableCollection<Color>( PresetColors );
			PossibleIcons = new ObservableCollection<BitmapImage>( PresetIcons );
			ActiveTimeSpans = new ObservableCollection<Interval<DateTime>>();	
		}


		/// <summary>
		///   Activates the activity.
		///   When it is the first activity activated, the current open windows will merge with the stored ones.
		/// </summary>
		[CommandExecute( Commands.ActivateActivity )]
		public void ActivateActivity( bool alsoOpen = true )
		{
			// Check whether activity is already active.
			if ( this == _overview.CurrentActivityViewModel )
			{
				// Interruptions still need to be opened.
				OpenInterruptions();

				// The event is still necessary to indicate the user is no longer selecting an activity.
				ActivatedActivityEvent( this );
				return;
			}

			ActivatingActivityEvent( this );

			// Activate. (model logic)
			if ( alsoOpen )
			{
				Activity.Activate();
			}
			else
			{
				Activity.View();
			}
			if ( ActiveTimeSpans == null )
			{
				ActiveTimeSpans = new ObservableCollection<Interval<DateTime>>();
			}
			DateTime now = DateTime.Now;
			_currentActiveTimeSpan = new Interval<DateTime>( now, now );
			ActiveTimeSpans.Add( _currentActiveTimeSpan );

			// It is important to first send out this event.
			ActivatedActivityEvent( this );

			// Initialize desktop.
			_desktopManager.SwitchToDesktop( _virtualDesktop );
			InitializeLibrary();

			OpenInterruptions();
		}

		void OpenInterruptions()
		{
			Activity.Interruptions
				.Where( i => !i.AttendedTo )
				.ForEach( i => i.Open() );
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
				case Mode.Activate:
					ActivateActivity( Activity.IsOpen );
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

		[CommandExecute( Commands.OpenActivity )]
		public void OpenActivity()
		{
			Activity.Open();
		}

		[CommandExecute( Commands.CloseActivity )]
		public void CloseActivity()
		{
			Deactivated();
			Activity.Close();
			ActivityClosedEvent( this );
		}

		[CommandExecute( Commands.Remove )]
		public void Remove()
		{
			CloseActivity();
			_overview.Remove( this );
		}

		[CommandCanExecute( Commands.Remove )]
		public bool CanRemoveActivity()
		{
			return !HasOpenWindows && !IsOpen;
		}

		public void UpdateHasOpenWindows()
		{
			HasOpenWindows = _virtualDesktop.Windows.Count > 0;
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

		public void Plan( DateTime atTime )
		{
			Occurance = atTime;
			TimeSpan = TimeSpan.FromHours( 1 );
			Activity.Plan( atTime, TimeSpan );
		}

		/// <summary>
		///   Merges the passed activity with this activity.
		///   TODO: For now only merging tasks with activities is supported, but this might need to be extended.
		/// </summary>
		/// <param name = "activity">The activity to merge with this activity.</param>
		public void Merge( ActivityViewModel activity )
		{
			// Make sure that the current selected libraries are up to date, prior to merging the folders of the other activity.
			UpdateLibrary();

			_overview.Remove( activity );
			Activity.Merge( activity.Activity );
			InitializeLibrary(); // The data paths of the merged activity need to be added to the library.

			// Merge the virtual desktops.
			if ( _overview.CurrentActivityViewModel == activity )
			{
				// A virtual desktop needs to be active at all times, so in case the current desktop is being merged, activate the target desktop.
				ActivateActivity( false );
			}
			_desktopManager.Merge( activity._virtualDesktop, _virtualDesktop );
		}

		/// <summary>
		///   Store activity context paths from the shell library to this activity. This only works when this activity is active.
		/// </summary>
		void UpdateLibrary()
		{
			if ( _overview.CurrentActivityViewModel != this )
			{
				return;
			}

			using ( var activityContext = ShellLibrary.Load( LibraryName, true ) )
			{
				var dataPaths = new List<Uri>();
				foreach ( var folder in activityContext )
				{
					dataPaths.Add( new Uri( folder.Path ) );
				}
				Activity.SetNewDataPaths( dataPaths );
			}
		}

		/// <summary>
		///   Initialize the library which contains all the context files.
		/// </summary>
		void InitializeLibrary()
		{
			// Initialize the shell library.
			// Information about Shell Libraries: http://msdn.microsoft.com/en-us/library/windows/desktop/dd758094(v=vs.85).aspx
			var dataPaths = Activity.GetUpdatedDataPaths().ToArray();

			if ( _overview.CurrentActivityViewModel == this )
			{
				using ( var activityContext = new ShellLibrary( LibraryName, true ) )
				{
					Array.ForEach( dataPaths, p => activityContext.Add( p.LocalPath ) );
				}
			}
		}

		public void Update( DateTime now )
		{
			IsPlannedActivity = Occurance > DateTime.Now;
			HasUnattendedInterruptions = Activity.Interruptions.Any( i => !i.AttendedTo );

			// Update the interval which indicates when the activity was open.
			if ( Activity.OpenIntervals.Count > 0 )
			{
				Occurance = Activity.OpenIntervals.First().Start;
				TimeSpan = Activity.OpenIntervals.Last().End - Activity.OpenIntervals.First().Start;
			}
			else
			{
				Occurance = Activity.DateCreated;
			}

			// Update the intervals which indicate when the activity was active.
			if ( _currentActiveTimeSpan != null )
			{
				_currentActiveTimeSpan.ExpandTo( now );
			}
		}

		/// <summary>
		///   Called by ActivityOverviewModel once the user has opened an activity other than this one, or when the application shuts down.
		/// </summary>
		internal void Deactivated()
		{
			if ( !IsActive )
			{
				return;
			}

			UpdateHasOpenWindows();
			UpdateLibrary();

			// Store activity context paths.
			// This can't be done in Persist(), since the same library is shared across activities.
			using ( var activityContext = ShellLibrary.Load( LibraryName, true ) )
			{
				var dataPaths = new List<Uri>();
				foreach ( var folder in activityContext )
				{
					dataPaths.Add( new Uri( folder.Path ) );
				}
				Activity.SetNewDataPaths( dataPaths );
			}

			Activity.Deactivate();
			_currentActiveTimeSpan = null;
		}

		public override void Persist()
		{
			// Nothing to do.
		}

		protected override void FreeUnmanagedResources()
		{
			// Nothing to do.
		}
	}
}
