using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ABC;
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
	[KnownType( typeof( WorkspaceSession ) )]
	[KnownType( typeof( ABC.Windows.Desktop.Window ) )]
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
		///   Event which is triggered when the activity is deactivated.
		/// </summary>
		public event ActivityEventHandler DeactivatedActivityEvent;

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
		///   Event which is triggered before activity suspension is started.
		/// </summary>
		public event ActivityEventHandler SuspendingActivityEvent;

		/// <summary>
		///   Event which is triggered after an activity has been suspended and no longer contains any open resources.
		/// </summary>
		public event ActivityEventHandler SuspendedActivityEvent;

		/// <summary>
		///   Event which is triggered when the activity changes from or to a to-do item.
		/// </summary>
		public event ActivityEventHandler ToDoChangedEvent;

		/// <summary>
		///   Event which is triggered when the activity is being removed.
		/// </summary>
		public event ActivityEventHandler RemovingActivityEvent;

		internal readonly Model.Activity Activity;

		readonly WorkspaceManager _workspaceManager;

		[DataMember]
		Workspace _workspace;

		/// <summary>
		///   The unique identifier of the activity this view model represents.
		/// </summary>
		public Guid Identifier
		{
			get { return Activity.Identifier; }
		}

		public bool IsUnnamed { get; set; }

		TimeInterval _currentActiveTimeSpan;

		bool _showActiveTimeSpans;

		public bool ShowActiveTimeSpans
		{
			set
			{
				_showActiveTimeSpans = value;
				foreach ( var workIntervalViewModel in WorkIntervals )
				{
					workIntervalViewModel.ShowActiveTimeSpans = _showActiveTimeSpans;
				}
			}
		}

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
			// Don't trigger a name change upon initializing the view model.
			if ( oldLabel == null )
			{
				return;
			}

			IsUnnamed = false;
			Activity.Name = newLabel;

			if ( _overview != null && _overview.CurrentActivityViewModel == this )
			{
				InitializeLibrary();
			}
		}

		/// <summary>
		///   Determines whether or not the activity is currently active (working on it).
		/// </summary>
		[NotifyProperty( Binding.Properties.IsActive )]
		public bool IsActive { get; private set; }

		/// <summary>
		///   Determines whether or not the activity is currently open, but not necessarily active (working on it).
		/// </summary>
		[NotifyProperty( Binding.Properties.IsOpen )]
		public bool IsOpen { get; private set; }

		/// <summary>
		///   Determines whether or not the activity is a to-do item, meaning that currently it is not open, nor is work planned on it in the future at a specific interval.
		///   When work will continue on the activity is undecided.
		/// </summary>
		[NotifyProperty( Binding.Properties.IsToDo )]
		public bool IsToDo { get; private set; }

		/// <summary>
		///   Determines whether or not the activity is a to-do item, or has a planned future interval.
		/// </summary>
		[NotifyProperty( Binding.Properties.IsPlanned )]
		public bool IsPlanned { get; private set; }

		/// <summary>
		///   Determines whether an activity contains old intervals, as opposed to only a planned part or to-do item.
		/// </summary>
		[NotifyProperty( Binding.Properties.ContainsHistory )]
		public bool CanRemovePlanning { get; private set; }

		/// <summary>
		///   Determines whether the activity is currently suspended, meaning it no longer takes up any resources.
		/// </summary>
		[NotifyProperty( Binding.Properties.IsSuspended )]
		[DataMember]
		public bool IsSuspended { get; private set; }

		/// <summary>
		///   Determines that the activity might be taking up resources since it has been activated, and hasn't been suspended since.
		/// </summary>
		[NotifyProperty( Binding.Properties.NeedsSuspension )]
		public bool NeedsSuspension { get; private set; }

		[NotifyProperty( Binding.Properties.HasUnattendedInterruptions )]
		public bool HasUnattendedInterruptions { get; private set; }

		[NotifyProperty( Binding.Properties.PossibleColors )]
		public ObservableCollection<Color> PossibleColors { get; private set; }

		[NotifyProperty( Binding.Properties.PossibleIcons )]
		public ObservableCollection<BitmapImage> PossibleIcons { get; private set; }

		[NotifyProperty( Binding.Properties.IsEditable )]
		public bool IsEditable { get; private set; }

		/// <summary>
		///   Collection of intervals which indicate when the activity was open, or when work is planned on it.
		///   TODO: The collection should only be allowed to be modified from the view model.
		/// </summary>
		[DataMember]
		[NotifyProperty( Binding.Properties.WorkIntervals )]
		public ObservableCollection<WorkIntervalViewModel> WorkIntervals { get; private set; }


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

		public ActivityViewModel( Model.Activity activity, WorkspaceManager workspaceManager )
			: this( activity, workspaceManager, workspaceManager.CreateEmptyWorkspace() ) {}

		public ActivityViewModel( Model.Activity activity, WorkspaceManager workspaceManager, Workspace workspace, bool isEditable = true )
		{
			Contract.Requires( activity != null );

			Activity = activity;

			_workspaceManager = workspaceManager;
			_workspace = workspace;

			Color = DefaultColor;
			IsEditable = isEditable;

			CommonInitialize();
		}

		public ActivityViewModel(
			Model.Activity activity,
			WorkspaceManager workspaceManager,
			ActivityViewModel storedViewModel )
		{
			Activity = activity;

			_workspaceManager = workspaceManager;
			_workspace = storedViewModel._workspace ?? workspaceManager.CreateEmptyWorkspace();
			NeedsSuspension = _workspace.HasResourcesToSuspend();

			Icon = storedViewModel.Icon;
			Color = storedViewModel.Color;
			IsSuspended = storedViewModel.IsSuspended;
			IsEditable = true;

			CommonInitialize();

			// Initialize all work intervals.
			// In case of planned intervals, all open intervals laying between the time the interval was planned, and an end of the planned interval, should not be shown on the timeline.
			var dontDisplay = Activity.PlannedIntervals.Select( p => new TimeInterval( p.PlannedAt, p.Interval.End ) ).ToList();
			var openIntervals = Activity.OpenIntervals
				.Where( i => !dontDisplay.Any( i.Intersects ) )
				.Select( interval => CreateWorkInterval( interval.Start, interval.End.Subtract( interval.Start ) ) )
				.ToList();

			var plannedIntervals = Activity.PlannedIntervals
				.Select( planned => planned.Interval )
				.Select( interval => CreateWorkInterval( interval.Start, interval.End.Subtract( interval.Start ), true ) );

			foreach ( var i in openIntervals.Concat( plannedIntervals ).OrderBy( i => i.Occurance ) )
			{
				WorkIntervals.Add( i );
			}

			// Update work intervals properties. They are ordered by date of occurance.
			for ( var i = 0; i < WorkIntervals.Count; i++ )
			{
				WorkIntervals[ i ].HeightPercentage = storedViewModel.WorkIntervals[ i ].HeightPercentage;
				WorkIntervals[ i ].OffsetPercentage = storedViewModel.WorkIntervals[ i ].OffsetPercentage;
				WorkIntervals[ i ].ActiveTimeSpans = storedViewModel.WorkIntervals[ i ].ActiveTimeSpans;
				WorkIntervals[ i ].ShowActiveTimeSpans = storedViewModel.WorkIntervals[ i ].ShowActiveTimeSpans;
			}

			_currentActiveTimeSpan = null;
		}

		void CommonInitialize()
		{
			IsActive = Activity.IsActive;
			IsOpen = Activity.IsOpen;
			Label = Activity.Name;
			IsToDo = Activity.IsToDo;

			Activity.ActivatedEvent += a => IsActive = true;
			Activity.DeactivatedEvent += a =>
			{
				IsActive = false;
				DeactivatedActivityEvent( this );
			};
			Activity.OpenedEvent += a => IsOpen = true;
			Activity.StoppedEvent += a =>
			{
				IsOpen = false;
				Deactivated();
				ActivityStoppedEvent( this );
			};
			Activity.ToDoChangedEvent += a =>
			{
				IsToDo = Activity.IsToDo;

				// Remove all future planned intervals.
				var toRemove = GetFutureWorkIntervals();
				foreach ( var r in toRemove )
				{
					WorkIntervals.Remove( r );
				}

				ToDoChangedEvent( this );
			};

			PossibleColors = new ObservableCollection<Color>( PresetColors );
			PossibleIcons = new ObservableCollection<BitmapImage>( PresetIcons );
			WorkIntervals = new ObservableCollection<WorkIntervalViewModel>();
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

			NeedsSuspension = true;
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

			// The only activity which can be active and does not have work intervals is home. Avoid adding active intervals.
			// TODO: Why would 'ActivityViewModel' need to be aware about a 'home' activity? This dependency should be removed.
			DateTime now = DateTime.Now;
			_currentActiveTimeSpan = new TimeInterval( now, now );
			if ( WorkIntervals.Count > 0 )
			{
				WorkIntervals.Last().ActiveTimeSpans.Add( _currentActiveTimeSpan );
			}

			// Initialize desktop.
			_workspaceManager.SwitchToWorkspace( _workspace );
			
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
			EditActivity( false );
		}

		public void EditActivity( bool focusPlannedInterval )
		{
			ActivityEditStartedEvent( this );

			var popup = new EditActivityPopup
			{
				DataContext = this,
				OccurancePicker = { IsOpen = focusPlannedInterval }
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
			if ( !CanOpenActivity() )
			{
				return;
			}

			IsOpen = true;
			bool hasPlannedParts = GetFutureWorkIntervals().Any();

			if ( WorkIntervals.Count == 0 || !hasPlannedParts )
			{
				WorkIntervals.Add( CreateWorkInterval() );
			}

			if ( IsActive )
			{
				var beforeLastWorkInterval = WorkIntervals[ WorkIntervals.Count - 2 ];
				var lastActiveTimeSpan = beforeLastWorkInterval.ActiveTimeSpans.Last();
				beforeLastWorkInterval.ActiveTimeSpans.Remove( lastActiveTimeSpan );
				WorkIntervals.Last().ActiveTimeSpans.Add( lastActiveTimeSpan );
			}

			Activity.Open();
		}

		[CommandCanExecute( Commands.OpenActivity )]
		public bool CanOpenActivity()
		{
			return !Activity.IsOpen;
		}

		[CommandExecute( Commands.StopActivity )]
		public void StopActivity()
		{
			Activity.Deactivate();
			Activity.Stop();
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
			// Be sure the activity is activated prior to suspending it.
			ActivateActivity();

			if ( IsSuspended )
			{
				return;
			}

			_isSuspending = true;
			SuspendingActivityEvent( this );

			// Start workspace suspension.
			_workspace.SuspendedWorkspace += OnSuspendedWorkspace;
			_workspace.Suspend();
		}

		void OnSuspendedWorkspace( AbstractWorkspace<WorkspaceSession> workspace )
		{
			_workspace.SuspendedWorkspace -= OnSuspendedWorkspace;
			IsSuspended = true;
			NeedsSuspension = false;
			_isSuspending = false;
			StopActivity();
			SuspendedActivityEvent( this );
		}

		[CommandCanExecute( Commands.SuspendActivity )]
		public bool CanSuspendActivity()
		{
			return IsEditable && !IsSuspended && !_isSuspending;
		}

		[CommandExecute( Commands.ForceSuspend )]
		public void ForceSuspend()
		{
			_workspace.ForceSuspend();
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
			_workspace.Resume();
		}

		[CommandExecute( Commands.Remove )]
		public void Remove()
		{
			RemovingActivityEvent( this );

			// TODO: Consider partial activity remove?
			StopActivity();
			_overview.Remove( this );
		}

		[CommandCanExecute( Commands.Remove )]
		public bool CanRemoveActivity()
		{
			return !NeedsSuspension;
		}

		[CommandExecute( Commands.MakeToDo )]
		public void MakeToDo()
		{
			Activity.MakeToDo();
		}

		[CommandCanExecute( Commands.MakeToDo )]
		public bool CanMakeToDo()
		{
			return !Activity.IsToDo;
		}

		[CommandExecute( Commands.RemovePlanning )]
		public void RemovePlanning()
		{
			Activity.RemovePlanning();

			foreach ( var i in GetFutureWorkIntervals() )
			{
				WorkIntervals.Remove( i );
			}

			// When no intervals are left, also remove the activity.
			if ( WorkIntervals.Count == 0 )
			{
				Remove();
			}
		}

		[CommandCanExecute( Commands.RemovePlanning )]
		public bool CanRemovePlanningCommand()
		{
			return CanRemovePlanning;
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
		///   Creates default 1 hour long planned activity.
		/// </summary>
		public void Plan( DateTime atTime )
		{
			StopActivity();

			DateTime at = atTime;
			TimeSpan duration = TimeSpan.FromHours( 1 );

			WorkIntervalViewModel plannedInterval = GetFutureWorkIntervals().FirstOrDefault();
			if ( plannedInterval == null )
			{
				try
				{
					Activity.AddPlannedInterval( at, duration );
				}
				catch ( InvalidOperationException )
				{
					// Planned too early, simply plan later.
					at = at + TimeSpan.FromMinutes( Model.Laevo.SnapToMinutes );
					Activity.AddPlannedInterval( at, duration );
				}
				WorkIntervals.Add( CreateWorkInterval( at, duration, true ) );
			}
			else
			{
				// TODO: Support replanning in model, rather than doing this through the view model.
				plannedInterval.Occurance = at;
			}
		}

		/// <summary>
		///   Return all planned work intervals which lie in the future.
		/// </summary>
		public List<WorkIntervalViewModel> GetFutureWorkIntervals()
		{
			return WorkIntervals.Where( i => i.IsPlanned && !i.IsPast() ).ToList();
		}

		/// <summary>
		///   Merges the passed activity with this activity.
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
				// One virtual desktop needs to be active at all times, so in case the current desktop is being merged, activate the target desktop.
				ActivateActivity( false );
			}
			else if ( _overview.CurrentActivityViewModel == this )
			{
				// The data paths of the merged activity need to be added to the library.
				InitializeLibrary();
			}
			_workspaceManager.Merge( activity._workspace, _workspace );

			// Only at the end, when nothing from the activity is 'active' anymore, convert it to the required state.
			if ( activity.IsToDo || activity.GetFutureWorkIntervals().Any() )
			{
				activity.RemovePlanning();
			}
			else
			{
				activity.StopActivity();
			}
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
					View.MessageBox.Show(
						"Something went wrong while initializing the Activity Context library, see whether this error still occurs when reopening this activity.",
						"Error initializing Activity Context Library",
						MessageBoxButton.OK,
						MessageBoxImage.Error );
				}
			}
		}

		public void Update( DateTime now )
		{
			HasUnattendedInterruptions = Activity.Interruptions.Any( i => !i.AttendedTo );
			IsPlanned = Activity.IsToDo || GetFutureWorkIntervals().Any();

			// Be sure not to allow removing planning when it would result in removal without suspension first.
			bool containsHistory = ( IsToDo && WorkIntervals.Count > 0 ) || WorkIntervals.Count > 1;
			CanRemovePlanning = IsPlanned && ( !NeedsSuspension || containsHistory );

			// Update the interval which indicates when the activity was open.
			if ( Activity.OpenIntervals.Count > 0 )
			{
				if ( IsOpen && !WorkIntervals.Last().IsPlanned )
				{
					WorkIntervals.Last().Occurance = Activity.OpenIntervals.Last().Start;
					WorkIntervals.Last().TimeSpan = Activity.OpenIntervals.Last().End - Activity.OpenIntervals.Last().Start;
				}
			}

			// Update the intervals which indicate when the activity was active.
			if ( _currentActiveTimeSpan != null )
			{
				_currentActiveTimeSpan = _currentActiveTimeSpan.ExpandTo( now );
				var lastWorkInterval = WorkIntervals.LastOrDefault();
				if ( lastWorkInterval != null )
				{
					ObservableCollection<TimeInterval> activeTimeSpans = lastWorkInterval.ActiveTimeSpans;
					activeTimeSpans[ activeTimeSpans.Count - 1 ] = _currentActiveTimeSpan;
				}
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

			// Storing activity context paths can't be done in Persist(), since the same library is shared across activities.
			UpdateLibrary();

			Activity.Deactivate();
			_currentActiveTimeSpan = null;
		}

		WorkIntervalViewModel CreateWorkInterval()
		{
			var newActivity = new WorkIntervalViewModel( this )
			{
				Occurance = DateTime.Now,
				ShowActiveTimeSpans = _showActiveTimeSpans
			};

			var lastInterval = WorkIntervals.LastOrDefault();
			if ( lastInterval != null )
			{
				newActivity.HeightPercentage = lastInterval.HeightPercentage;
				newActivity.OffsetPercentage = lastInterval.OffsetPercentage;
			}

			return newActivity;
		}

		WorkIntervalViewModel CreateWorkInterval( DateTime occurence, TimeSpan timeSpan, bool isPlanned = false )
		{
			var newActivity = CreateWorkInterval();
			newActivity.Occurance = occurence;
			newActivity.TimeSpan = timeSpan;
			newActivity.IsPlanned = isPlanned;

			return newActivity;
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