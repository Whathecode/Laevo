using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using ABC.Applications.Persistence;
using ABC.Interruptions;
using ABC.Workspaces;
using ABC.Workspaces.Library;
using ABC.Workspaces.Windows;
using ABC.Workspaces.Windows.Settings;
using Laevo.Data.Model;
using Laevo.Data.View;
using Laevo.Logging;
using Laevo.Model.AttentionShifts;
using Laevo.Peer;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview;
using NLog;
using Whathecode.System;
using Whathecode.System.Extensions;
using Whathecode.System.Management;
using Whathecode.System.Windows.Threading;


namespace Laevo.Model
{
	/// <summary>
	///   The main model of the Laevo Virtual Desktop Manager.
	/// </summary>
	/// <author>Steven Jeuris</author>
	public class Laevo
	{
		static readonly Logger Log = LogManager.GetCurrentClassLogger();

		public const string DefaultActivityName = "New Activity";
		public const string DefaultTaskName = "New Task";

		readonly Dispatcher _dispatcher;

		readonly ProcessTracker _processTracker = new ProcessTracker();

		/// <summary>
		///   Triggered when the user returns from the logon screen to the desktop.
		///   HACK: This is required due to a bug in WPF. More info can be found in MainViewModel.RecoverFromGuiCrash().
		/// </summary>
		public event Action LogonScreenExited;

		readonly AbstractInterruptionTrigger _interruptionTrigger;
		readonly IModelRepository _dataRepository;

		public IUsersPeer UsersPeer { get; private set; }

		public static string ProgramLocalDataFolder { get; private set; }

		public IWindowOperations WindowClipboard { get; private set; }

		/// <summary>
		///   The controller which allows switching between different workspaces.
		/// </summary>
		public WorkspaceManager WorkspaceManager { get; private set; }

		public event Action<Activity> ActivityRemoved;
		public event Action<Activity> InvitedToActivity;
		public event Action<Activity> InterruptionAdded;

		public ReadOnlyCollection<AbstractAttentionShift> AttentionShifts
		{
			get { return _dataRepository.AttentionShifts; }
		}

		public Settings Settings
		{
			get { return _dataRepository.Settings; }
		}

		public Activity HomeActivity { get; private set; }

		Activity _currentActivity;

		public Activity CurrentActivity
		{
			get { return _currentActivity; }
			private set
			{
				_currentActivity = value;
				Log.InfoWithData( "Current activity changed.", new LogData( _currentActivity ) );
			}
		}

		Activity _openTimeLine;
		bool _isHierarchyView; // TODO: Should this be managed here? This is currently also duplicated elsewhere.
		readonly List<Activity> _visibleActivities = new List<Activity>();


		public Laevo( string dataFolder, IModelRepository dataRepository, AbstractInterruptionTrigger interruptionTrigger,
			PersistenceProvider persistenceProvider, AbstractPeerFactory peerFactory )
		{

            ServiceLocator.GetInstance().RegisterService( this );

			Log.Info( "Startup." );

			_dispatcher = Dispatcher.CurrentDispatcher;

			_interruptionTrigger = interruptionTrigger;
			_dataRepository = dataRepository;
			UsersPeer = peerFactory.UsersPeer;

			// When invited to an activity, add it to the home activity.
		    UsersPeer.Invited += activity => AddActivity( activity );

			ProgramLocalDataFolder = dataFolder;

			// Initialize desktop manager.
			string vdmSettingsPath = Path.Combine( dataFolder, "VdmSettings" );
			if ( !Directory.Exists( vdmSettingsPath ) )
			{
				Directory.CreateDirectory( vdmSettingsPath );
			}
			var vdmSettings = new LoadedSettings();
			foreach ( string file in Directory.EnumerateFiles( vdmSettingsPath ) )
			{
				try
				{
					using ( var stream = new FileStream( file, FileMode.Open ) )
					{
						vdmSettings.AddSettingsFile( stream );
					}
				}
				catch ( InvalidOperationException )
				{
					// Simply ignore invalid files.
				}
			}
			var vdmManager = new VirtualDesktopManager( vdmSettings, persistenceProvider );
			WindowClipboard = vdmManager; // Only expose window clipboard, WorkspaceManager is used to expose workspaces.
			Log.Debug( "Desktop manager initialized." );

			// Initialize shell library manager.
			var libraryManager = new LibraryManager( "Activity Context" );
			Log.Debug( "Library manager initialized." );

			// Initialize workspace manager.
			WorkspaceManager = new WorkspaceManager( new [] { vdmManager.NonGeneric, libraryManager.NonGeneric } );
			Log.Debug( "Workspace manager initialized." );

			// Find home activity and set as current activity and visible time line.
			HomeActivity = _dataRepository.HomeActivity;
			ChangeVisibleTimeLine( HomeActivity );
			HomeActivity.View();

			// Set up interruption handlers.
			_interruptionTrigger.InterruptionReceived += interruption =>
			{
				// TODO: For now all interruptions lead to new activities, but later they might be added to existing activities.
				Log.InfoWithData( "Incoming interruption.", new LogData( "Type", interruption.GetType() ) );
				var newActivity = _dataRepository.CreateNewActivity( interruption.Name, HomeActivity );
				newActivity.MakeToDo();
				newActivity.AddInterruption( interruption );
				DispatcherHelper.SafeDispatch( _dispatcher, () =>
				{
					HandleActivity( newActivity );
					InterruptionAdded( newActivity );
					// TODO: This event should probably be removed and some other mechanism should be used.
				} );
			};

			// Start tracking processes.
			_processTracker.Start();
			_processTracker.ProcessStopped += p =>
			{
				// TODO: Improved verification, rather than just name.
				if ( p.Name == "LogonUI.exe" )
				{
					Log.Info( "Returned from log on screen." );
					LogonScreenExited();
				}
			};

			_dataRepository.AddAttentionShift( new ApplicationAttentionShift( ApplicationAttentionShift.Application.Startup ) );
		}


		public const int SnapToMinutes = 15;

		public static DateTime GetNearestTime( DateTime near )
		{
			return near.Round( DateTimePart.Minute ).SafeSubtract( TimeSpan.FromMinutes( near.Minute % SnapToMinutes ) );
		}

		public void Update( DateTime now )
		{
			_visibleActivities.ForEach( a => a.Update( now ) );
			_interruptionTrigger.Update( now );
		}

	    public void AddActivity( Activity activity, Activity parent = null)
	    {
            // Make sure this activity is not yet managed by the repository.
            if (_dataRepository.ContainsActivity(activity)) return;

            Log.InfoWithData("Invited to activity.", new LogData(activity));

            _dataRepository.AddActivity(activity, parent ?? HomeActivity);
            HandleActivity(activity);
            InvitedToActivity(activity);
            // TODO: Similar to interruption events, this event should probably be removed and some other mechanism should be used.
	    }

		/// <summary>
		///   Create and add a new activity.
		/// </summary>
		/// <returns>The newly created activity.</returns>
		public Activity CreateNewActivity( Activity parent, string name = DefaultActivityName )
		{
			var activity = _dataRepository.CreateNewActivity( name, parent );
			Log.InfoWithData( "New activity.", new LogData( activity ) );
			HandleActivity( activity );

			return activity;
		}

		/// <summary>
		///   Moves an activity to a new parent.
		/// </summary>
		/// <param name="activity">The activity to move.</param>
		/// <param name="toParent">The parent activity to move the activity to.</param>
		public void MoveActivity( Activity activity, Activity toParent )
		{
			Contract.Requires( activity != toParent );

			_dataRepository.MoveActivity( activity, toParent );
		}

		public void ChangeVisibleTimeLine( Activity activity )
		{
			_visibleActivities.ForEach( UnHandleActivity );
			_visibleActivities.Clear();

			_isHierarchyView = true;
			_openTimeLine = activity;
			Log.InfoWithData( "Switched to hierarchy view.", new LogData( activity ) );
			_dataRepository.GetActivities( activity ).ForEach( HandleActivity );
		}

		public void ChangeToPersonalTimeLine()
		{
			_visibleActivities.ForEach( UnHandleActivity );
			_visibleActivities.Clear();

			_isHierarchyView = false;
			Log.InfoWithData( "Switched to personal view." );
			_dataRepository.GetPersonalActivities().ForEach( HandleActivity );
		}

		public Dictionary<Activity, List<AbstractInterruption>> GetUnattendedInterruptions()
		{
			return _dataRepository.GetUnattendedInterruptions();
		}

		void HandleActivity( Activity activity )
		{
			// TODO: Optimize when needed, 'GetActivities' is called quite a lot, just because parent/child relationship is only maintained in repository.
			bool hierarchyAndVisible = _isHierarchyView && _dataRepository.GetActivities( _openTimeLine ).Contains( activity );
			bool personalAndOwned = !_isHierarchyView && activity.OwnedUsers.Contains( _dataRepository.User );
			if ( hierarchyAndVisible || personalAndOwned )
			{
				_visibleActivities.Add( activity );
				activity.ActivatedEvent += OnActivityActivated;
			}
		}

		void UnHandleActivity( Activity activity )
		{
			activity.ActivatedEvent -= OnActivityActivated;
		}

		void OnActivityActivated( Activity activity )
		{
			_dataRepository.AddAttentionShift( new ActivityAttentionShift( activity ) );
			CurrentActivity = activity;
		}

		/// <summary>
		///   Remove a task or activity.
		/// </summary>
		/// <param name = "activity">The task or activity to remove.</param>
		public void Remove( Activity activity )
		{
			Log.InfoWithData( "Remove activity.", new LogData( activity ) );

			activity.ActivatedEvent -= OnActivityActivated;

			AttentionShifts.OfType<ActivityAttentionShift>()
				.Where( s => activity.Equals( s.Activity ) )
				.ForEach( a => a.ActivityRemoved() );

			_dataRepository.RemoveActivity( activity );

			ActivityRemoved( activity );
		}

		public Activity CreateNewTask( Activity parent, string name = DefaultTaskName )
		{
			var task = _dataRepository.CreateNewActivity( name, parent );
			Log.InfoWithData( "New task.", new LogData( task ) );
			task.MakeToDo();
			HandleActivity( task );

			return task;
		}

		public void SwapTaskOrder( Activity task1, Activity task2 )
		{
			Log.InfoWithData( "Swap task order.", new LogData( "Task 1", task1 ), new LogData( "Task 2", task2 ) );

			_dataRepository.SwapTaskOrder( task1, task2 );
		}

		public void Exit()
		{
			WorkspaceManager.Close();
			_dataRepository.AddAttentionShift( new ApplicationAttentionShift( ApplicationAttentionShift.Application.Shutdown ) );
			_processTracker.Stop();

			Persist();

			Log.Info( "Exited." );
		}

		public void Persist()
		{
			Log.Debug( "Persisting." );

			_dataRepository.SaveChanges();
		}
	}
}