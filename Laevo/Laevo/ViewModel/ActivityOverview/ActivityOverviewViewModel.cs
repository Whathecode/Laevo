using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Timers;
using Laevo.ViewModel.Activity;
using Laevo.ViewModel.ActivityOverview.Binding;
using VirtualDesktopManager;
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

		readonly Model.Laevo _model;
		readonly DesktopManager _desktopManager = new DesktopManager();		

		/// <summary>
		///   Timer used to update data regularly.
		/// </summary>
		readonly Timer _updateTimer = new Timer( 1000 );

		/// <summary>
		///   The ViewModel of the activity which is currently open.
		/// </summary>
		public ActivityViewModel CurrentActivityViewModel { get; private set; }

		/// <summary>
		///   The ViewModel of the activity which is currently selected.
		///   This means it is being edited, or more information is requested about it. It doesn't mean it's open!
		/// </summary>
		public ActivityViewModel SelectedActivityViewModel { get; private set; }

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

			// Hook up timer.
			_updateTimer.Elapsed += UpdateData;
			_updateTimer.Start();

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
				ActivityViewModel viewModel;
				if ( _model.CurrentActivity == activity )
				{
					// Ensure current (first) activity is assigned to the correct desktop.					
					viewModel = new ActivityViewModel( activity, _desktopManager, _desktopManager.CurrentDesktop )
					{
						Icon = ActivityViewModel.HomeIcon,
						Label = activity.Name
					};
					CurrentActivityViewModel = viewModel;
					viewModel.OpenActivity();
				}
				else if ( existingActivities.ContainsKey( activity.DateCreated ) )
				{
					viewModel = new ActivityViewModel( activity, _desktopManager, existingActivities[ activity.DateCreated ] );
				}
				else
				{
					viewModel = new ActivityViewModel( activity, _desktopManager );
				}

				viewModel.OpenedActivityEvent += OnActivityOpened;
				viewModel.SelectedActivityEvent += OnActivitySelected;
				Activities.Add( viewModel );
			}		
		}

		~ActivityOverviewViewModel()
		{
			_updateTimer.Stop();
			_desktopManager.Close();
		}


		/// <summary>
		///   Create a new activity and open it.
		/// </summary>
		public void NewActivity()
		{
			var newActivity = new ActivityViewModel( _model.CreateNewActivity(), _desktopManager );
			Activities.Add( newActivity );

			newActivity.OpenActivity();
		}

		void OnActivityOpened( ActivityViewModel viewModel )
		{
			CurrentActivityViewModel = viewModel;
			OpenedActivityEvent( viewModel );
		}

		void OnActivitySelected( ActivityViewModel viewModel )
		{
			SelectedActivityViewModel = viewModel;
		}

		void UpdateData( object sender, ElapsedEventArgs e )
		{
			CurrentTime = DateTime.Now;

			// Update model.
			_model.Update();

			// Update required view models.
			Activities.ForEach( a => a.Update() );
		}

		public override void Persist()
		{
			Activities.ForEach( a => a.Persist() );

			using ( var activityFileStream = new FileStream( ActivitiesFile, FileMode.Create ) )
			{
				ActivitySerializer.WriteObject( activityFileStream, Activities.ToDictionary( a => a.DateCreated, a => a ) );
			}
		}
	}
}