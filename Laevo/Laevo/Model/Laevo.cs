using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using ABC.Applications.Persistence;
using ABC.Interruptions;
using ABC.PInvoke.Process;
using ABC.Windows.Desktop;
using ABC.Windows.Desktop.Settings;
using Laevo.Data;
using Laevo.Data.Model;
using Laevo.Model.AttentionShifts;
using Whathecode.System;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Threading;


namespace Laevo.Model
{
	/// <summary>
	///   The main model of the Laevo Virtual Desktop Manager.
	///   TODO: Make tasks typesafe?
	/// </summary>
	/// <author>Steven Jeuris</author>
	public class Laevo
	{
		public const string DefaultActivityName = "New Activity";

		readonly Dispatcher _dispatcher;

		readonly ProcessTracker _processTracker = new ProcessTracker();

		/// <summary>
		///   Triggered when the user returns from the logon screen to the desktop.
		///   HACK: This is required due to a bug in WPF. More info can be found in MainViewModel.RecoverFromGuiCrash().
		/// </summary>
		public event Action LogonScreenExited;

		readonly AbstractInterruptionTrigger _interruptionTrigger;
		readonly IModelRepository _dataRepository;

		public static string ProgramLocalDataFolder { get; private set; }

		public VirtualDesktopManager DesktopManager { get; private set; }

		public event Action<Activity> ActivityRemoved;

		public ReadOnlyCollection<Activity> Activities
		{
			get { return _dataRepository.Activities; }
		}

		public event Action<Activity> InterruptionAdded;

		public ReadOnlyCollection<Activity> Tasks
		{
			get { return _dataRepository.Tasks; }
		}

		public ReadOnlyCollection<AbstractAttentionShift> AttentionShifts
		{
			get { return _dataRepository.AttentionShifts; }
		}

		public Settings Settings
		{
			get { return _dataRepository.Settings; }
		}

		public Activity HomeActivity { get; private set; }
		public Activity CurrentActivity { get; private set; }


		public Laevo( string dataFolder, IModelRepository dataRepository, AbstractInterruptionTrigger interruptionTrigger, PersistenceProvider persistenceProvider )
		{
			_dispatcher = Dispatcher.CurrentDispatcher;

			_interruptionTrigger = interruptionTrigger;
			_dataRepository = dataRepository;

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
			DesktopManager = new VirtualDesktopManager( vdmSettings, persistenceProvider );

			// Find home activity and set as current activity.
			HomeActivity = _dataRepository.HomeActivity;
			CurrentActivity = HomeActivity;

			// Handle activities and tasks from previous sessions.
			_dataRepository
				.Activities
				.Concat( _dataRepository.Tasks )
				.Concat( new[] { HomeActivity } )
				.ForEach( HandleActivity );

			// Set up interruption handlers.
			_interruptionTrigger.InterruptionReceived += interruption =>
			{
				// TODO: For now all interruptions lead to new activities, but later they might be added to existing activities.
				var newActivity = _dataRepository.CreateNewTask( interruption.Name );
				newActivity.AddInterruption( interruption );
				DispatcherHelper.SafeDispatch( _dispatcher, () =>
				{
					HandleActivity( newActivity );
					InterruptionAdded( newActivity ); // TODO: This event should probably be removed and some other mechanism should be used.
				} );
			};

			// Start tracking processes.
			_processTracker.Start();
			_processTracker.ProcessStopped += p =>
			{
				// TODO: Improved verification, rather than just name.
				if ( p.Name == "LogonUI.exe" )
				{
					LogonScreenExited();
				}
			};

			_dataRepository.AddAttentionShift( new ApplicationAttentionShift( ApplicationAttentionShift.Application.Startup ) );
		}


		public const int SnapToMinutes = 15;

		public static DateTime GetNearestTime( DateTime near )
		{
			const int snapToMinutes = 15;

			return near.Round( DateTimePart.Minute ).SafeSubtract( TimeSpan.FromMinutes( near.Minute % snapToMinutes ) );
		}

		public void Update( DateTime now )
		{
			Activities.ForEach( a => a.Update( now ) );
			_interruptionTrigger.Update( now );
		}

		/// <summary>
		///   Create and add a new activity and sets it as the current activity.
		/// </summary>
		/// <returns>The newly created activity.</returns>
		public Activity CreateNewActivity( string name = DefaultActivityName )
		{
			var activity = _dataRepository.CreateNewActivity( name );
			HandleActivity( activity );

			CurrentActivity = activity;
			return activity;
		}

		void HandleActivity( Activity activity )
		{
			activity.ActivatedEvent += OnActivityActivated;
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
			activity.ActivatedEvent -= OnActivityActivated;

			AttentionShifts.OfType<ActivityAttentionShift>().Where( s => s.Activity == activity ).ForEach( a => a.ActivityRemoved() );

			_dataRepository.RemoveActivity( activity );

			ActivityRemoved( activity );
		}

		public Activity CreateNewTask()
		{
			var task = _dataRepository.CreateNewTask();
			HandleActivity( task );

			return task;
		}

		public void CreateActivityFromTask( Activity task )
		{
			_dataRepository.CreateActivityFromTask( task );
		}

		public void CreateTaskFromActivity( Activity activity )
		{
			_dataRepository.CreateTaskFromActivity( activity );
		}

		public void AddTask( Activity task )
		{
			HandleActivity( task );
			_dataRepository.AddTask( task );
		}

		public void SwapTaskOrder( Activity task1, Activity task2 )
		{
			_dataRepository.SwapTaskOrder( task1, task2 );
		}

		public void Exit()
		{
			_dataRepository.AddAttentionShift( new ApplicationAttentionShift( ApplicationAttentionShift.Application.Shutdown ) );

			_processTracker.Stop();

			try
			{
				_dataRepository.SaveChanges();
			}
			catch ( PersistenceException pe )
			{
				MessageBox.Show( pe.Message, "Saving data failed", MessageBoxButton.OK );
			}
		}
	}
}