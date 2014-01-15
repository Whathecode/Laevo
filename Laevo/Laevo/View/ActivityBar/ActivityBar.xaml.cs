using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Laevo.ViewModel.Activity;
using Microsoft.Win32;
using Whathecode.System.Extensions;


namespace Laevo.View.ActivityBar
{
	/// <summary>
	/// Interaction logic for ActivityBar.xaml
	/// </summary>
	public partial class ActivityBar
	{
		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		public static extern IntPtr DefWindowProc( IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam );

		const int WindowsHitTest = 0x0084;
		const int HitBorder = 18;
		const int HitBottomBorder = 15;
		const int HitBottomleftBorderCorner = 16;
		const int HitBottomRightBorderCorner = 17;
		const int HitLeftBorder = 10;
		const int HitRightBorder = 11;
		const int HitTopBorder = 12;
		const int HitTopLeftBorderCorner = 13;
		const int HitTopRightBorderCorner = 14;

		[DllImport( "dwmapi.dll", EntryPoint = "#127" )]
		static extern void DwmGetColorizationParameters( out Dwmcolorizationparams parameters );

		public struct Dwmcolorizationparams
		{
			public uint
				ColorizationColor,
				ColorizationAfterglow,
				ColorizationColorBalance,
				ColorizationAfterglowBalance,
				ColorizationBlurBalance,
				ColorizationGlassReflectionIntensity,
				ColorizationOpaqueBlend;
		}

		const string BtnActivityName = "BtnActivity";
		const double TopWhenVisible = -3;

		readonly TimeSpan _displayTime = TimeSpan.FromSeconds( 4 );
		readonly DoubleAnimation _hideAnimation;

		int _selectionIndex;
		ActivityViewModel _selectedActivity;

		public bool BarGotClosed { get; private set; }

		public ActivityBar()
		{
			InitializeComponent();

			ResizeToScreenWidth();
			SystemEvents.DisplaySettingsChanged += ( s, a ) => ResizeToScreenWidth();

			Loaded += OnLoaded;
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
		}


		/// <summary>
		/// Position window so that the borders aren't visible and it looks like the taskbar.
		/// </summary>
		void ResizeToScreenWidth()
		{
			Left = -5;
			Width = SystemParameters.PrimaryScreenWidth + 10;
		}

		/// <summary>
		/// Gets a color from windows registry in order to apply it to a window in both Aero and othere themes. (Not used for now)
		/// </summary>
		Color GetWindowColorizationColor( bool opaque )
		{
			Dwmcolorizationparams windowsColors;
			DwmGetColorizationParameters( out windowsColors );

			return Color.FromArgb(
				(byte)( opaque ? 255 : windowsColors.ColorizationColor >> 24 ),
				(byte)( windowsColors.ColorizationColor >> 16 ),
				(byte)( windowsColors.ColorizationColor >> 8 ),
				(byte)windowsColors.ColorizationColor );
		}

		void OnLoaded( object sender, RoutedEventArgs e )
		{
			// Disable default resize behavior by overriding default events.
			var mainWindowPointer = new WindowInteropHelper( this ).Handle;
			var mainWindowSource = HwndSource.FromHwnd( mainWindowPointer );
			if ( mainWindowSource != null )
			{
				mainWindowSource.AddHook( HandleWindowHits );
			}
		}

		/// <summary>
		/// Override the window hit test. If the cursor is over a resize border, return a standard border result instead.
		/// </summary>
		IntPtr HandleWindowHits( IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled )
		{
			if ( message != WindowsHitTest )
			{
				return IntPtr.Zero;
			}

			handled = true;
			var hitLocation = DefWindowProc( hwnd, message, wParam, lParam ).ToInt32();
			switch ( hitLocation )
			{
				case HitBottomBorder:
				case HitBottomleftBorderCorner:
				case HitBottomRightBorderCorner:
				case HitLeftBorder:
				case HitRightBorder:
				case HitTopBorder:
				case HitTopLeftBorderCorner:
				case HitTopRightBorderCorner:
					hitLocation = HitBorder;
					break;
			}

			return new IntPtr( hitLocation );
		}

		/// <summary>
		/// Show the activity bar and hide it after some time.
		/// </summary>
		public void ShowActivityBar( bool activate )
		{
			SetSelectionStartIndex();

			ShowBarFor( _displayTime );

			if ( activate )
			{
				Activate();
				TbActivityName.Select( 0, TbActivityName.Text.Length );
				TbActivityName.Focus();
			}
		}

		void ShowBarFor( TimeSpan delay )
		{
			PinTaskbar();

			// Hide the activity bar after some time if it's not activated.
			if ( !IsActive || BarGotClosed )
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

		void HideCompleted( object sender, EventArgs e )
		{
			Hide(); // Hide the window entirely since it is no longer visible.
		}

		void OnDeactivated( object sender, EventArgs e )
		{
			// Force activity name binding to update.
			var nameBinding = TbActivityName.GetBindingExpression( TextBox.TextProperty );
			if ( nameBinding != null && !TbActivityName.IsReadOnly && TbActivityName.IsEnabled )
			{
				nameBinding.UpdateSource();
			}

			// Hide the infobox.
			if ( !BarGotClosed )
			{
				_hideAnimation.BeginTime = TimeSpan.Zero;
				BeginAnimation( TopProperty, _hideAnimation );
			}
			BarGotClosed = false;
		}

		void LabelKeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key.EqualsAny( Key.Enter, Key.Escape ) )
			{
				BarGotClosed = true;

				// Pass focus to next element.
				TbActivityName.MoveFocus( new TraversalRequest( FocusNavigationDirection.Next ) );

				ShowBarFor( TimeSpan.Zero );
			}
		}

		/// <summary>
		/// Activates selected activity.
		/// </summary>
		public void ActivateSelectedActivity()
		{
			if ( _selectedActivity != null )
			{
				//Hide();
				BarGotClosed = true;

				//Pass focus to previous element.
				TbActivityName.MoveFocus( new TraversalRequest( FocusNavigationDirection.Previous ) );

				_selectedActivity.ActivateActivity( false );
				_selectedActivity = null;
			}
		}

		/// <summary>
		/// Switches beetewn current and opened activities and then saves selected activity.
		/// </summary>
		public void SelectNextActivity()
		{
			if ( ItemsControlActivities.Items.Count > 0 )
			{
				// Come back on the beginning when selection index is outside of activities collection.
				if ( _selectionIndex == ItemsControlActivities.Items.Count )
				{
					_selectionIndex = 0;
				}
				// Gets the content presented for the activity item.
				var contentPresenter = (ContentPresenter)ItemsControlActivities.ItemContainerGenerator.ContainerFromIndex( _selectionIndex );
				// Gets the selected activity button and give it a focus.
				var activityButton = contentPresenter.ContentTemplate.FindName( BtnActivityName, contentPresenter ) as Button;
				// ReSharper disable once PossibleNullReferenceException, checked in the first line, never will be null.
				activityButton.Focus();

				_selectedActivity = (ActivityViewModel)ItemsControlActivities.Items[ _selectionIndex ];
				_selectionIndex++;
			}
		}

		/// <summary>
		/// Sets selection start index for switching between activites.
		/// </summary>
		void SetSelectionStartIndex()
		{
			if ( ItemsControlActivities.Items.Count > 0 )
			{
				// Start index depends on active activity, if it is home activity we want to start selction from first open activity. 
				// On the other case we want to skip first open activity in a first loop.
				var firstInList = (ActivityViewModel)ItemsControlActivities.Items[ 0 ];
				_selectionIndex = firstInList.IsActive ? 1 : 0;
			}
			else
			{
				_selectionIndex = 0;
			}
		}
	}
}