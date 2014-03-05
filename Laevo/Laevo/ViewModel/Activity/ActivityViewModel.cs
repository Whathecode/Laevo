using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ABC.Windows;
using ABC.Windows.Desktop;
using Laevo.Model.AttentionShifts;
using Laevo.View.Activity;
using Laevo.ViewModel.ActivityOverview;
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
	[KnownType( typeof( ABC.Windows.Window ) )]
	public class ActivityViewModel : AbstractViewModel
	{
		ActivityOverviewViewModel _overview;

		const string IconResourceLocation = "view/activity/icons";
		public static List<BitmapImage> PresetIcons { get; private set; }

		public static readonly List<Color> PresetColors = new List<Color>
		{
			Color.FromRgb( 86, 124, 212 ), // Blue
			Color.FromRgb( 121, 234, 255 ), // Cyan
			Color.FromRgb( 88, 160, 2 ), // Green
			Color.FromRgb( 227, 220, 0 ), // Yellow
			Color.FromRgb( 212, 131, 0 ), // Orange
			Color.FromRgb( 212, 50, 38 ), // Red
			Color.FromRgb( 193, 75, 159 ), // Purple
			Color.FromRgb( 193, 217, 197 ), // Gray/White
			Color.FromRgb( 49, 54, 52 ) // Dark gray
		};

		public static readonly Color DefaultColor = PresetColors[ 0 ];

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
		///   Event which is triggered when activity is stopped.
		/// </summary>
		public event ActivityEventHandler ActivityStoppedEvent;

		/// <summary>
		///   Event which is triggered when activity is opended.
		/// </summary>
		public event ActivityEventHandler ActivityOpenedEvent;

		/// <summary>
		///   Event which is triggered before activity suspension is started.
		/// </summary>
		public event ActivityEventHandler SuspendingActivityEvent;

		/// <summary>
		///   Event which is triggered after an activity has been suspended and no longer contains any open resources.
		/// </summary>
		public event ActivityEventHandler SuspendedActivityEvent;

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
			IsUnnamed = false;
			Activity.Name = newLabel;

			if ( _overview != null && _overview.CurrentActivityViewModel == this )
			{
				InitializeLibrary();
			}
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

		/// <summary>
		///   Determines whether the activity is currently suspended, meaning it no longer takes up any resources.
		/// </summary>
		[NotifyProperty( Binding.Properties.IsSuspended )]
		[DataMember]
		public bool IsSuspended { get; private set; }

		[NotifyProperty( Binding.Properties.HasUnattendedInterruptions )]
		public bool HasUnattendedInterruptions { get; private set; }

		[NotifyProperty( Binding.Properties.PossibleColors )]
		public ObservableCollection<Color> PossibleColors { get; set; }

		[NotifyProperty( Binding.Properties.PossibleIcons )]
		public ObservableCollection<BitmapImage> PossibleIcons { get; set; }

		[NotifyProperty( Binding.Properties.IsEditable )]
		public bool IsEditable { get; private set; }

		public bool IsUnnamed { get; set; }


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
		}

		public ActivityViewModel( Model.Activity activity, VirtualDesktopManager desktopManager )
			: this( activity, desktopManager, desktopManager.CreateEmptyDesktop() ) {}

		public ActivityViewModel( Model.Activity activity, VirtualDesktopManager desktopManager, VirtualDesktop desktop, bool isEditable = true )
		{
			Contract.Requires( activity != null );

			Activity = activity;

			_desktopManager = desktopManager;
			_virtualDesktop = desktop;

			Label = activity.Name;
			Color = DefaultColor;
			HeightPercentage = 0.2;
			OffsetPercentage = 1;
			IsEditable = isEditable;

			CommonInitialize();
		}

		public ActivityViewModel(
			Model.Activity activity,
			VirtualDesktopManager desktopManager,
			ActivityViewModel storedViewModel,
			IEnumerable<ActivityAttentionShift> activitySwitches )
		{
			Activity = activity;

			_desktopManager = desktopManager;
			_virtualDesktop = storedViewModel._virtualDesktop ?? desktopManager.CreateEmptyDesktop();

			Label = activity.Name;
			Icon = storedViewModel.Icon;
			Color = storedViewModel.Color;
			HeightPercentage = storedViewModel.HeightPercentage;
			OffsetPercentage = storedViewModel.OffsetPercentage;
			IsSuspended = storedViewModel.IsSuspended;
			IsEditable = true;

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
			Activity.StoppedEvent += a => IsOpen = false;

			PossibleColors = new ObservableCollection<Color>( PresetColors );
			PossibleIcons = new ObservableCollection<BitmapImage>( PresetIcons );
			ActiveTimeSpans = new ObservableCollection<Interval<DateTime>>();
		}


		/// <summary>
		///   Sets the overview which this activity is displayed in.
		///   TODO: Before constructor injection was used, but no longer possible due to IViewRepository.
		///         The dependency between ActivityViewModel and ActivityOverviewModel needs to be properly investigated.
		/// </summary>
		/// <param name = "overview">The overview which this activity is displayed in.</param>
		public void SetOverviewManager( ActivityOverviewViewModel overview )
		{
			_overview = overview;
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

			// Initialize desktop.
			try
			{
				_desktopManager.SwitchToDesktop( _virtualDesktop );
			}
			catch ( UnresponsiveWindowsException e )
			{
				var unresponsive = e.UnresponsiveWindows.GroupBy( u => u.Window.GetProcess().ProcessName ).ToList();

				// Ask user whether to ignore the locking application windows from now on.
				// TODO: The error message could be made topmost when we could access the overview window. This exception might need to be propagated to the view.
				string error = unresponsive.Aggregate(
					"The following applications stopped responding and are locking up the window manager:\n\n",
					( info, processWindows ) => info + "- " + processWindows.Key + "\n" );
				error += "\nWould you like to ignore them from now on?";
				MessageBoxResult result = MessageBox.Show( error, "Unresponsive Applications", MessageBoxButton.YesNo, MessageBoxImage.Exclamation );
				if ( result == MessageBoxResult.Yes )
				{
					e.IgnoreAllWindows();
				}
			}
			InitializeLibrary();

			OpenInterruptions();

			// Resume the activity in case it was suspended.
			if ( IsSuspended )
			{
				ResumeActivity();
			}

			ActivatedActivityEvent( this );
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
			if ( _overview.ActivityMode.HasFlag( Mode.Select ) )
			{
				SelectedActivityEvent( this );
			}
			else
			{
				// TODO: When the activity is in a suspended state, ask whether the user would like to open and resume it. In order to open the activity it needs to be resumed.
				ActivateActivity( Activity.IsOpen );
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

		[CommandCanExecute( Commands.EditActivity )]
		public bool CanEditActivity()
		{
			return IsEditable;
		}

		[CommandExecute( Commands.OpenActivity )]
		public void OpenActivity()
		{
			Activity.Open();
			ActivityOpenedEvent( this );
		}

		[CommandCanExecute( Commands.OpenActivity )]
		public bool CanOpenActivity()
		{
			return !Activity.IsOpen;
		}

		[CommandExecute( Commands.StopActivity )]
		public void StopActivity()
		{
			Deactivated();
			Activity.Stop();
			ActivityStoppedEvent( this );
		}

		[CommandCanExecute( Commands.StopActivity )]
		public bool CanStopActivity()
		{
			return IsEditable && Activity.IsOpen && !_isSuspending;
		}

		bool _isSuspending;
		[CommandExecute( Commands.SuspendActivity )]
		public void SuspendActivity()
		{
			if ( IsSuspended )
			{
				return;
			}

			_isSuspending = true;
			SuspendingActivityEvent( this );

			// Await full suspension in background.
			var awaitSuspend = new BackgroundWorker();
			awaitSuspend.DoWork += ( sender, args ) =>
			{
				do
				{
					// When all windows are closed, assume suspension was successful.
					_desktopManager.UpdateWindowAssociations();
					Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
				}
				while ( _virtualDesktop.Windows.Count != 0 );
			};
			awaitSuspend.RunWorkerCompleted += ( sender, args ) =>
			{
				IsSuspended = true;
				_isSuspending = false;
				StopActivity();
				SuspendedActivityEvent( this );
			};
			awaitSuspend.RunWorkerAsync();

			// Initiate the actual suspension.
			_virtualDesktop.Suspend();
		}

		[CommandCanExecute( Commands.SuspendActivity )]
		public bool CanSuspendActivity()
		{
			return IsEditable && !IsSuspended && !_isSuspending;
		}

		[CommandExecute( Commands.ForceSuspend )]
		public void ForceSuspend()
		{
			// By moving the remaining windows to the home activity, suspension will finish.
			_virtualDesktop.TransferWindows( _virtualDesktop.Windows.ToList(), _overview.HomeActivity._virtualDesktop );
		}

		[CommandCanExecute( Commands.ForceSuspend )]
		public bool CanForceSuspend()
		{
			return _isSuspending;
		}

		public void ResumeActivity()
		{
			if ( !IsSuspended )
			{
				return;
			}

			IsSuspended = false;
			_virtualDesktop.Resume();
		}

		[CommandExecute( Commands.Remove )]
		public void Remove()
		{
			StopActivity();
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
			// Merging with itself is useless.
			if ( activity == this )
			{
				return;
			}

			// Make sure that the current selected libraries are up to date, prior to merging the folders of the other activity.
			UpdateLibrary();

			Activity.Merge( activity.Activity );

			// Ensure the correct activity is activated and its initialized properly.
			if ( _overview.CurrentActivityViewModel == activity )
			{
				// A virtual desktop needs to be active at all times, so in case the current desktop is being merged, activate the target desktop.
				ActivateActivity( false );
			}
			else if ( _overview.CurrentActivityViewModel == this )
			{
				// The data paths of the merged activity need to be added to the library.
				InitializeLibrary();
			}
			_desktopManager.Merge( activity._virtualDesktop, _virtualDesktop );

			// Only at the end, when nothing from the activity is 'active' anymore, remove it.
			_overview.Remove( activity );
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
		///   Initialize the library which contains all the context files. This should only be called when the activity is currently active.
		/// </summary>
		void InitializeLibrary()
		{
			// Initialize the shell library.
			// Information about Shell Libraries: http://msdn.microsoft.com/en-us/library/windows/desktop/dd758094(v=vs.85).aspx
			var dataPaths = Activity.GetUpdatedDataPaths().ToArray();

			using ( var activityContext = new ShellLibrary( LibraryName, true ) )
			{
				// TODO: Optionally set the icon of the library to the icon of the activity? For now, just set it to the icon of the executing assembly.
				activityContext.IconResourceId = new IconReference( Assembly.GetExecutingAssembly().Location, 0 );

				int retries = 5;
				var pathsToAdd = new List<Uri>();
				pathsToAdd.AddRange( dataPaths );
				while ( pathsToAdd.Count > 0 && retries > 0 )
				{
					foreach ( Uri path in dataPaths )
					{
						try
						{
							activityContext.Add( path.LocalPath );
							pathsToAdd.Remove( path );
						}
						catch ( COMException )
						{
							// TODO: How to handle/prevent the COMException which is sometimes thrown?
							// System.Runtime.InteropServices.COMException (0x80070497): Unable to remove the file to be replaced.
						}
						finally
						{
							--retries;
						}
					}
				}
				if ( pathsToAdd.Count > 0 )
				{
					MessageBox.Show(
						"Something went wrong while initializing the Activity Context library, see whether this error still occurs when reopening this activity.",
						"Error initializing Activity Context Library",
						MessageBoxButton.OK,
						MessageBoxImage.Error );
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