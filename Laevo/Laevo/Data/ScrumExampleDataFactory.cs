using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using ABC.Windows.Desktop;
using Laevo.Data.Model;
using Laevo.Data.View;
using Laevo.Model;
using Laevo.ViewModel.Activity;
using Whathecode.System.Extensions;


namespace Laevo.Data
{
	class ScrumExampleDataFactory : IDataFactory
	{
		readonly ScrumModelRepository _modelRepository;


		public ScrumExampleDataFactory()
		{
			_modelRepository = new ScrumModelRepository();
		}


		public IModelRepository CreateModelRepository()
		{
			return _modelRepository;
		}

		public IViewRepository CreateViewRepository( IModelRepository linkedModelRepository, VirtualDesktopManager desktopManager )
		{
			return new ScrumViewRepository( _modelRepository, desktopManager );
		}
	}


	class ScrumModelRepository : AbstractMemoryModelRepository
	{
		readonly TimeSpan _sprintLenght = TimeSpan.FromDays( 14 );


		readonly List<Activity> _sprints = new List<Activity>();
		public IReadOnlyCollection<Activity> Sprints
		{
			get { return _sprints.AsReadOnly(); }
		}

		readonly List<Tuple<Activity, BitmapImage>> _productBacklog = new List<Tuple<Activity, BitmapImage>>();
		public IReadOnlyCollection<Tuple<Activity, BitmapImage>> ProductBacklog
		{
			get { return _productBacklog.AsReadOnly(); }
		}


		public ScrumModelRepository()
		{
			HomeActivity = new Activity( "Home" );

			// Reuseable reference data.
			DateTime now = DateTime.Now;
			var firstSprintDate = now.Round( DayOfWeek.Monday );
			BitmapImage uiIcon = ActivityViewModel.PresetIcons.First( i => i.UriSource.AbsolutePath.Contains( "window.png" ) );
			BitmapImage featureIcon = ActivityViewModel.PresetIcons.First( i => i.UriSource.AbsolutePath.Contains( "tag.png" ) );
			BitmapImage bugIcon = ActivityViewModel.PresetIcons.First( i => i.UriSource.AbsolutePath.Contains( "burn.png" ) );

			// Project overview. (sprints + product backlog)
			DateTime start = firstSprintDate;
			for ( int i = 1; i < 5; ++i )
			{
				CreateSprint( i, start );
				start += _sprintLenght;
			}
			CreateUserStory( "Transfer payment", featureIcon );
			CreateUserStory( "Ability to tip", featureIcon );
			CreateUserStory( "Incorrect total amount due", bugIcon );
			CreateUserStory( "Improve transaction dialog", uiIcon );
			CreateUserStory( "Show overview of transactions", uiIcon );
			CreateUserStory( "List past transactions", featureIcon );
		}


		void CreateSprint( int i, DateTime start )
		{
			var sprint = new Activity( "Sprint " + i );
			sprint.Plan( start, _sprintLenght );
			MemoryActivities.Add( sprint );
			_sprints.Add( sprint );
		}

		void CreateUserStory( string name, BitmapImage icon )
		{
			var userStory = new Activity( name );
			MemoryTasks.Add( userStory );
			_productBacklog.Add( Tuple.Create( userStory, icon ) );
		}

		public override void SaveChanges()
		{
			// No need to save changes.
		}
	}


	class ScrumViewRepository : AbstractMemoryViewRepository
	{
		public ScrumViewRepository( ScrumModelRepository modelRepository, VirtualDesktopManager desktopManager )
		{
			// Create view models for sprints.
			foreach ( var activity in modelRepository.Sprints )
			{
				var viewModel = new ActivityViewModel( activity, desktopManager );
				viewModel.ChangeIcon( ActivityViewModel.PresetIcons.First( i => i.UriSource.AbsolutePath.Contains( "flag.png" ) ) );
				viewModel.ChangeColor( ActivityViewModel.PresetColors[ 7 ] );
				Activities.Add( viewModel );
			}

			// Create view model for user stories.
			foreach ( var userStory in modelRepository.ProductBacklog )
			{
				var viewModel = new ActivityViewModel( userStory.Item1, desktopManager );
				viewModel.ChangeIcon( userStory.Item2 );
				viewModel.ChangeColor( ActivityViewModel.PresetColors[ 0 ] );
				Tasks.Add( viewModel );
			}
		}


		public override void SaveChanges()
		{
			// No need to save changes.
		}
	}
}
