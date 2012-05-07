using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Timers;
using Laevo.Model.AttentionShifts;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview.Binding;
using VirtualDesktopManager;
using Whathecode.System.Arithmetic.Range;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Aspects.ViewModel;


namespace Laevo.ViewModel.ActivityOverview
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	class ActivityOverviewViewModel : AbstractViewModel
	{
		static readonly string ActivitiesFile = Path.Combine( Model.Laevo.ProgramDataFolder, "ActivityRepresentations.xml" );


		/// <summary>
		///   Event which is triggered when an activity is opened.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler OpenedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is selected.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler SelectedActivityEvent;

		/// <summary>
		///   Event which is triggered when an activity is closed.
		/// </summary>
		public event ActivityViewModel.ActivityEventHandler ClosedActivityEvent;

		readonly Model.Laevo _model;
		readonly DesktopManager _desktopManager = new DesktopManager();

		/// <summary>
		///   Timer used to update data regularly.
		/// </summary>
		readonly Timer _updateTimer = new Timer( 100 );

		[NotifyProperty( Binding.Properties.TimeLineRenderScale )]
		public float TimeLineRenderScale { get; set; }

		/// <summary>
		///   The mode determines which actions are possible within the activity overview.
		/// </summary>
		[NotifyProperty( Binding.Properties.Mode )]
		public Mode ActivityMode { get; set; }

		/// <summary>
		///   The ViewModel of the activity which is currently open.
		/// </summary>
		public ActivityViewModel CurrentActivityViewModel { get; private set; }

		[NotifyProperty( Binding.Properties.CurrentTime )]
		public DateTime CurrentTime { get; private set; }

		[NotifyProperty( Binding.Properties.Activities )]
		public ObservableCollection<ActivityViewModel> Activities { get; private set; }

		static readonly DataContractSerializer ActivitySerializer = new DataContractSerializer(
			typeof( Dictionary<DateTime, ActivityViewModel> ),
			null, Int32.MaxValue, true, false,
			new DataContractSurrogate() );


		public ActivityOverviewViewModel( Model.Laevo model )
		{
			_model = model;

			// Setup desktop manager.
			// TODO: NullReferenceException in the lambda. w != null
			_desktopManager.AddWindowFilter( w => !(w.GetProcess().ProcessName.StartsWith( "Laevo" ) && w.GetClassName().Contains( "Laevo" )) );

			// Check for stored presentation options for existing activities.
			var existingActivities = new Dictionary<DateTime, ActivityViewModel>();
			if ( File.Exists( ActivitiesFile ) )
			{
				using ( var activityFileStream = new FileStream( ActivitiesFile, FileMode.Open ) )
				{
					existingActivities = (Dictionary<DateTime, ActivityViewModel>)ActivitySerializer.ReadObject( activityFileStream );
				}
			}

			// Initialize a view model for all activities.
			Activities = new ObservableCollection<ActivityViewModel>();
			foreach ( var activity in _model.Activities )
			{
				bool isFirstActivity = _model.CurrentActivity == activity;

				// Create view model.
				ActivityViewModel viewModel;
				if ( isFirstActivity )
				{
					// Ensure current (first) activity is assigned to the correct desktop.
					viewModel = new ActivityViewModel( this, activity, _desktopManager )
					{
						Icon = ActivityViewModel.HomeIcon,
						Color = ActivityViewModel.DefaultColor,
						HeightPercentage = 0.2,
						OffsetPercentage = 1
					};					
				}
				else if ( existingActivities.ContainsKey( activity.DateCreated ) )
				{
					// Activities from previous sessions.
					// Find the attention shifts which occured while the activity was open.
					ReadOnlyCollection<Interval<DateTime>> openIntervals = activity.OpenIntervals;
					var attentionShifts = _model.AttentionShifts
						.OfType<ActivityAttentionShift>()
						.Where( shift => openIntervals.Any( i => i.LiesInInterval( shift.Time ) ) );

					viewModel = new ActivityViewModel(
						this, activity, _desktopManager,
						existingActivities[ activity.DateCreated ],
						attentionShifts );
				}
				else
				{
					// Newly added activities at startup.
					viewModel = new ActivityViewModel( this, activity, _desktopManager );
				}
				HookActivityEvents( viewModel );

				Activities.Add( viewModel );

				// The first activity needs to be opened at startup.
				if ( isFirstActivity )
				{
					viewModel.OpenActivity();					
				}
			}

			// Hook up timer.
			_updateTimer.Elapsed += UpdateData;
			_updateTimer.Start();
		}


		/// <summary>
		///   Create a new activity and open it.
		/// </summary>
		public void NewActivity()
		{
			var newActivity = new ActivityViewModel( this, _model.CreateNewActivity(), _desktopManager );
			lock ( Activities )
			{
				Activities.Add( newActivity );
			}

			HookActivityEvents( newActivity );
			newActivity.OpenActivity();
		}

		void HookActivityEvents( ActivityViewModel activity )
		{
			activity.OpenedActivityEvent += OnActivityOpened;
			activity.SelectedActivityEvent += OnActivitySelected;
			activity.ActivityEditStartedEvent += a => ActivityMode = Mode.Edit;
			activity.ActivityEditFinishedEvent += a => ActivityMode = Mode.Open;
			activity.ActivityClosedEvent += OnActivityClosed;
		}

		void OnActivityOpened( ActivityViewModel viewModel )
		{
			// Indicate an activity is no longer active (visible).
			if ( CurrentActivityViewModel != null && viewModel != CurrentActivityViewModel )
			{
				CurrentActivityViewModel.Deactivated();
			}

			CurrentActivityViewModel = viewModel;
			OpenedActivityEvent( viewModel );
		}

		void OnActivityClosed( ActivityViewModel viewModel )
		{
			CurrentActivityViewModel = null;
			ClosedActivityEvent( viewModel );
		}

		void OnActivitySelected( ActivityViewModel viewModel )
		{
			SelectedActivityEvent( viewModel );
		}

		void UpdateData( object sender, ElapsedEventArgs e )
		{
			CurrentTime = e.SignalTime;

			// Update model.
			_model.Update( CurrentTime );

			// Update required view models.
			lock ( Activities )
			{
				if ( Activities != null )
				{
					Activities.ForEach( a => a.Update( CurrentTime ) );
				}
			}
		}

		public override void Persist()
		{
			lock ( Activities )
			{
				Activities.ForEach( a => a.Persist() );
			}

			using ( var activityFileStream = new FileStream( ActivitiesFile, FileMode.Create ) )
			{
				ActivitySerializer.WriteObject( activityFileStream, Activities.ToDictionary( a => a.DateCreated, a => a ) );
			}
		}

		protected override void FreeUnmanagedResources()
		{
			_updateTimer.Stop();
			Activities.ForEach( a => a.Dispose() );

			_desktopManager.Close();
		}
	}
}