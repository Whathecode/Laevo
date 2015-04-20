using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ABC.Workspaces;
using ABC.Workspaces.Library;
using Laevo.View.Activity;
using Laevo.ViewModel.ActivityOverview;
using Laevo.ViewModel.User;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;
using Color = System.Windows.Media.Color;
using Commands = Laevo.ViewModel.Activity.Binding.Commands;


namespace Laevo.ViewModel.Activity
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	[DataContract]
	[KnownType( typeof( BitmapImage ) )]
	[KnownType( typeof( WorkspaceSession ) )]
	[KnownType( typeof( ABC.Window ) )]
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
		public static readonly BitmapImage DefaultIcon;

		public delegate void ActivityEventHandler( ActivityViewModel viewModel );

		/// <summary>
		///   Event which is triggered at the start when an activity is being activated when it wasn't activated before.
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

			// Attempt renaming the specific folder of the activity.
			if ( IsAccessible ) // Do not rename merged folders, as not to break any links created while merging.
			{
				string oldFolder = Activity.SpecificFolder.LocalPath;
				string newFolder = Activity.UpdateSpecificFolder().LocalPath;

				// Update the Windows Shell Library paths to the new specific folder.
				if ( oldFolder != newFolder )
				{
					Library library = _workspace.GetInnerWorkspace<Library>();
					List<string> paths = library.Paths.ToList();
					paths.Remove( oldFolder );
					paths.Add( newFolder );
					library.SetPaths( paths );
				}
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

		[DataMember]
		[NotifyProperty( Binding.Properties.IsEditable )]
		public bool IsEditable { get; set; }

		[DataMember]
		[NotifyProperty( Binding.Properties.IsAccessible )]
		public bool IsAccessible { get; private set; }

		/// <summary>
		///   Collection of intervals which indicate when the activity was open, or when work is planned on it.
		///   TODO: The collection should only be allowed to be modified from the view model.
		/// </summary>
		[DataMember]
		[NotifyProperty( Binding.Properties.WorkIntervals )]
		public ObservableCollection<WorkIntervalViewModel> WorkIntervals { get; private set; }

		/// <summary>
		///   The interval denoting the first time the activity was open or planned (start of first work interval) to the last time it was (end of last work interval).
		///   When the activity does not contain any work intervals (e.g. to-do item) this value is null.
		/// </summary>
		[NotifyProperty( Binding.Properties.OpenInterval )]
		public Interval<DateTime, TimeSpan> OpenInterval { get; private set; }

		ObservableCollection<UserViewModel> _accessUsers;
		ReadOnlyObservableCollection<UserViewModel> _readOnlyAccessUsers;

		/// <summary>
		///   The users who have access to the time line of this activity.
		/// </summary>
		[NotifyProperty( Binding.Properties.AccessUsers )]
		public ReadOnlyObservableCollection<UserViewModel> AccessUsers
		{
			get { return _readOnlyAccessUsers; }
		}

		EditActivityPopup _editActivityPopup;


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
		}

		public ActivityViewModel( Model.Activity activity, WorkspaceManager workspaceManager )
			: this( activity, workspaceManager, workspaceManager.CreateEmptyWorkspace() ) {}

		public ActivityViewModel( Model.Activity activity, WorkspaceManager workspaceManager, Workspace workspace )
		{
			Contract.Requires( activity != null );

			Activity = activity;
			_workspaceManager = workspaceManager;
			_workspace = workspace;

			IsEditable = true;
			IsAccessible = true;
			Color = DefaultColor;
			Icon = DefaultIcon;

			CommonInitialize();
		}

		public ActivityViewModel( Model.Activity activity, WorkspaceManager workspaceManager, ActivityViewModel storedViewModel )
		{
			Activity = activity;

			_workspaceManager = workspaceManager;
			_workspace = storedViewModel._workspace ?? workspaceManager.CreateEmptyWorkspace();
			NeedsSuspension = _workspace.HasResourcesToSuspend();
			IsSuspended = storedViewModel.IsSuspended;

			IsEditable = storedViewModel.IsEditable;
			IsAccessible = storedViewModel.IsAccessible;
			Color = storedViewModel.Color;
			Icon = storedViewModel.Icon;

			CommonInitialize();

			// Initialize all work intervals.
			// In case of planned intervals, all open intervals laying between the time the interval was planned, and an end of the planned interval, should not be shown on the time line.
			List<TimeInterval> dontDisplay = Activity.PlannedIntervals.Select( p => new TimeInterval( p.PlannedAt, p.Interval.End ) ).ToList();
			List<WorkIntervalViewModel> openIntervals = Activity.OpenIntervals
				.Where( i => !dontDisplay.Any( i.Intersects ) )
				.Select( interval => CreateWorkInterval( interval.Start, interval.End.Subtract( interval.Start ) ) )
				.ToList();
			IEnumerable<WorkIntervalViewModel> plannedIntervals = Activity.PlannedIntervals
				.Select( planned => planned.Interval )
				.Select( interval => CreateWorkInterval( interval.Start, interval.End.Subtract( interval.Start ), true ) );
			foreach ( var i in openIntervals.Concat( plannedIntervals ).OrderBy( i => i.Occurance ) )
			{
				WorkIntervals.Add( i );
			}

			// Update work intervals properties. They are ordered by date of occurrence.
			for ( var i = 0; i < WorkIntervals.Count; i++ )
			{
				WorkIntervals[ i ].HeightPercentage = storedViewModel.WorkIntervals[ i ].HeightPercentage;
				WorkIntervals[ i ].OffsetPercentage = storedViewModel.WorkIntervals[ i ].OffsetPercentage;
				WorkIntervals[ i ].ActiveTimeSpans = storedViewModel.WorkIntervals[ i ].ActiveTimeSpans;
				WorkIntervals[ i ].ShowActiveTimeSpans = storedViewModel.WorkIntervals[ i ].ShowActiveTimeSpans;
			}
		}

		void CommonInitialize()
		{
			IsActive = Activity.IsActive;
			IsOpen = Activity.IsOpen;
			Label = Activity.Name;
			IsToDo = Activity.IsToDo;

			// Set Windows Shell Library folder.
			Library library = _workspace.GetInnerWorkspace<Library>();
			List<string> paths = library.Paths.ToList();
			if ( !paths.Contains( Activity.SpecificFolder.LocalPath ) )
			{
				paths.Add( Activity.SpecificFolder.LocalPath );
			}
			library.SetPaths( paths );

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
			WorkIntervals.CollectionChanged += ( sender, args ) => UpdateOpenInterval();

			// Initialize users who have access to this time line.
			_accessUsers = new ObservableCollection<UserViewModel>();
			_readOnlyAccessUsers = new ReadOnlyObservableCollection<UserViewModel>( _accessUsers );
			Activity.AccessUsers.ForEach( u => _accessUsers.Add( new UserViewModel( u ) ) );
			Activity.AccessAddedEvent += ( a, u ) => _accessUsers.Add( new UserViewModel( u ) );
			Activity.AccessRemovedEvent += ( a, u ) => _accessUsers.Remove( new UserViewModel( u ) );
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

			// Update active time spans.
			DateTime now = DateTime.Now;
			_currentActiveTimeSpan = new TimeInterval( now, now );
			if ( !IsToDo )
			{
				WorkIntervals.Last().ActiveTimeSpans.Add( _currentActiveTimeSpan );
			}

			// Initialize desktop.
			_workspaceManager.SwitchToWorkspace( _workspace );

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
			_workspace.GetInnerWorkspace<Library>().Open();
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
				if ( _overview.ActivityMode.HasFlag( Mode.Activate ) )
				{
					ActivateActivity( Activity.IsOpen );
				}
				else
				{
					OpenTimeLine();
				}
			}
		}

		[CommandCanExecute( Commands.SelectActivity )]
		public bool CanSelectActivity()
		{
			// Merged activities can not be accessed later on.
			return IsAccessible;
		}

		[CommandExecute( Commands.EditActivity )]
		public void EditActivity()
		{
			EditActivity( false );
		}


		public void EditActivity( bool focusPlannedInterval )
		{
			ActivityEditStartedEvent( this );
			_editActivityPopup = new EditActivityPopup
			{
				DataContext = this,
				OccurancePicker = { IsOpen = focusPlannedInterval }
			};
			_editActivityPopup.Closed += ( s, a ) =>
			{
				_editActivityPopup = null;
				ActivityEditFinishedEvent( this );
			};

			_editActivityPopup.ShowDialog();
		}

		[CommandCanExecute( Commands.EditActivity )]
		public bool CanEditActivity()
		{
			return IsEditable && _editActivityPopup == null;
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

			Activity.Open();
		}

		[CommandExecute( Commands.OpenTimeLine )]
		public void OpenTimeLine()
		{
			_overview.LoadActivities( this );
		}

		[CommandCanExecute( Commands.OpenActivity )]
		public bool CanOpenActivity()
		{
			return !Activity.IsOpen && IsAccessible;
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
			return IsEditable && Activity.IsOpen && !_isSuspending && IsOverviewActive();
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

		[CommandCanExecute( Commands.OpenActivityLibrary )]
		public bool IsOverviewActive()
		{
			return !_overview.IsActive;
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
			Activity.Stop();

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

			// Activities which have been merged elsewhere become inaccessible, thus merging to them is not possible.
			if ( !IsAccessible )
			{
				throw new InvalidOperationException( "Can not merge to an activity which has been merged elsewhere." );
			}

			Activity.Merge( activity.Activity );

			// Ensure the correct activity is activated and its initialized properly.
			if ( _overview.CurrentActivityViewModel == activity )
			{
				// One workspace needs to be active at all times, so in case the current workspace is being merged, activate the target workspace.
				ActivateActivity( false );
			}
			_workspaceManager.Merge( activity._workspace, _workspace );

			// Only at the end, when nothing from the activity is 'active' anymore, convert it to the required state.
			if ( activity.IsToDo || activity.GetFutureWorkIntervals().Any() )
			{
				activity.RemovePlanning();

				// When no intervals are left, also remove the activity.
				if ( activity.WorkIntervals.Count == 0 )
				{
					activity.Remove();
				}
			}
			else
			{
				activity.StopActivity();
			}

			// Merged activities are no longer accessible.
			activity.IsAccessible = false;
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
					TimeInterval lastOpen = Activity.OpenIntervals.Last();
					WorkIntervalViewModel viewModel = WorkIntervals.Last();
					viewModel.Occurance = lastOpen.Start;
					viewModel.TimeSpan = lastOpen.Size;
					OpenInterval = OpenInterval.ExpandTo( lastOpen.End );
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
					if ( activeTimeSpans.Count > 0 )
					{
						activeTimeSpans[ activeTimeSpans.Count - 1 ] = _currentActiveTimeSpan;
					}
				}
			}
		}

		void UpdateOpenInterval()
		{
			WorkIntervalViewModel first = WorkIntervals.FirstOrDefault();
			if ( first != null )
			{
				WorkIntervalViewModel last = WorkIntervals.Last();
				OpenInterval = new Interval<DateTime, TimeSpan>( first.Occurance, last.Occurance + last.TimeSpan );
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

			Activity.Deactivate();
			_currentActiveTimeSpan = null;
		}

		WorkIntervalViewModel CreateWorkInterval()
		{
			var newInterval = new WorkIntervalViewModel( this )
			{
				Occurance = DateTime.Now,
				ShowActiveTimeSpans = _showActiveTimeSpans
			};

			if ( IsActive )
			{
				newInterval.ActiveTimeSpans.Add( _currentActiveTimeSpan );
			}

			var lastInterval = WorkIntervals.LastOrDefault();
			if ( lastInterval != null )
			{
				newInterval.HeightPercentage = lastInterval.HeightPercentage;
				newInterval.OffsetPercentage = lastInterval.OffsetPercentage;
			}

			return newInterval;
		}

		WorkIntervalViewModel CreateWorkInterval( DateTime occurence, TimeSpan timeSpan, bool isPlanned = false )
		{
			var newActivity = CreateWorkInterval();
			newActivity.Occurance = occurence;
			newActivity.TimeSpan = timeSpan;
			newActivity.IsPlanned = isPlanned;

			return newActivity;
		}

		public void InviteUser( UserViewModel user )
		{
			Activity.AddAccess( user.User );
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