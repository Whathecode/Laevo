using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Laevo.ViewModel.Activity;
using Microsoft.Win32;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;


namespace Laevo.View.ActivityBar
{
	/// <summary>
	/// Interaction logic for ActivityBar.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	public partial class ActivityBar
	{
		public enum Properties
		{
			SelectedActivity,
			CurrentActivity
		}

		const double TopWhenVisible = -5;

		readonly TimeSpan _displayTime = TimeSpan.FromSeconds( 4 );
		readonly DoubleAnimation _hideAnimation;

		bool _barGotClosed;
		readonly ActivityMenu _activityMenu = new ActivityMenu();

		[DependencyProperty( Properties.CurrentActivity )]
		public ActivityViewModel CurrentActivity { get; set; }

		[DependencyPropertyChanged( Properties.CurrentActivity )]
		public static void OnCurrentActivityChanged( DependencyObject sender, DependencyPropertyChangedEventArgs args )
		{
			var bar = (ActivityBar)sender;

			if ( bar.CurrentActivity != null && bar.CurrentActivity.IsUnnamed )
			{
				bar.CurrentActivity.IsUnnamed = false;
				bar.Activate();
				bar.Focus();

				bar.ActivityName.Select( 0, bar.ActivityName.Text.Length );
				bar.ActivityName.Focus();
			}
		}

		[DependencyProperty( Properties.SelectedActivity )]
		public ActivityViewModel SelectedActivity { get; set; }


		public ActivityBar()
		{
			InitializeComponent();

			// Set up two way binding for the necessary properties to the viewmodel.
			var selectedBinding = new Binding( "SelectedActivity" ) { Mode = BindingMode.TwoWay };
			SetBinding( WpfControlAspect<Properties>.GetDependencyProperty( Properties.SelectedActivity ), selectedBinding );
			var currentBinding = new Binding( "Overview.CurrentActivityViewModel" );
			SetBinding( WpfControlAspect<Properties>.GetDependencyProperty( Properties.CurrentActivity ), currentBinding );

			ResizeToScreenWidth();
			SystemEvents.DisplaySettingsChanged += ( s, a ) => ResizeToScreenWidth();

			Deactivated += OnDeactivated;

			Activated += ( s, args ) => PinTaskbar();

			// Create animation which hides the activity bar, sliding it out of view.
			_hideAnimation = new DoubleAnimation
			{
				From = TopWhenVisible,
				To = -( Height + 5 ),
				Duration = new Duration( TimeSpan.FromSeconds( 0.5 ) ),
				FillBehavior = FillBehavior.Stop,
				BeginTime = _displayTime
			};

			// When menu is deactivated, hide entire activity bar.
			_activityMenu.Deactivated += ( sender, args ) =>
			{
				_activityMenu.Hide();
				ShowBarFor( TimeSpan.Zero );
			};
		}


		/// <summary>
		/// Position window so that the borders aren't visible and it looks like the taskbar.
		/// </summary>
		void ResizeToScreenWidth()
		{
			// Sets width of Activity Bar to half of the screen and places it in the middle.
			Width = SystemParameters.PrimaryScreenWidth / 2;
			Left = ( SystemParameters.PrimaryScreenWidth / 2 ) - ( Width / 2 );

			_activityMenu.Left = Left;
			_activityMenu.Top = Height + TopWhenVisible;
		}

		/// <summary>
		/// Show the activity bar and hide it after some time.
		/// </summary>
		public void ShowActivityBar( bool autoHide )
		{
			// When the bar is reshown, hide menu.
			_activityMenu.Hide();

			if ( autoHide )
			{
				ShowBarFor( _displayTime );
			}
			else
			{
				PinTaskbar();
			}
		}

		public void HideActivityBar()
		{
			ShowBarFor( _displayTime );
		}

		void ShowBarFor( TimeSpan delay )
		{
			PinTaskbar();

			// Hide the activity bar after some time if it's not activated.
			if ( !IsInUse() )
			{
				_hideAnimation.BeginTime = delay;
				BeginAnimation( TopProperty, _hideAnimation );
			}
		}

		public void PinTaskbar()
		{
			_hideAnimation.Completed -= HideCompleted;
			BeginAnimation( TopProperty, null );
			Top = TopWhenVisible;
			_hideAnimation.Completed += HideCompleted;

			Show();
		}

		/// <summary>
		/// Determines whether or not the activity bar is currently in use.
		/// </summary>
		public bool IsInUse()
		{
			return ( IsActive || _activityMenu.IsActive ) && !_barGotClosed;
		}

		void HideCompleted( object sender, EventArgs e )
		{
			Hide(); // Hide the window entirely since it is no longer visible.
		}

		void OnDeactivated( object sender, EventArgs e )
		{
			// Force activity name binding to update.
			var nameBinding = ActivityName.GetBindingExpression( TextBox.TextProperty );
			if ( nameBinding != null && !ActivityName.IsReadOnly && ActivityName.IsEnabled )
			{
				nameBinding.UpdateSource();
			}

			// Hide the infobox when needed.
			if ( !_barGotClosed && !_activityMenu.IsVisible )
			{
				_hideAnimation.BeginTime = TimeSpan.Zero;
				BeginAnimation( TopProperty, _hideAnimation );
			}
			_barGotClosed = false;
		}

		void LabelKeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key.EqualsAny( Key.Enter, Key.Escape ) )
			{
				_barGotClosed = true;

				ActivityButton.Focus();
				ShowBarFor( TimeSpan.Zero );

				e.Handled = true;
			}
		}

		void SwitchActivityMenu( object sender, RoutedEventArgs e )
		{
			if ( _activityMenu.IsVisible )
			{
				_activityMenu.Hide();
				return;
			}
			
			_activityMenu.DataContext = CurrentActivity;
			_activityMenu.Show();
		}

		void OnActivityHover( object sender, MouseEventArgs e )
		{
			var button = (FrameworkElement)sender;
			SelectedActivity = (ActivityViewModel)button.DataContext;
		}

		void OnActivityHoverLeave( object sender, MouseEventArgs e )
		{
			SelectedActivity = null;
		}
	}
}