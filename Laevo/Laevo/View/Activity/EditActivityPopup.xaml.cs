using System;
using System.Windows;
using Whathecode.System;
using Whathecode.System.Extensions;


namespace Laevo.View.Activity
{
	/// <summary>
	/// Interaction logic for EditActivityPopup.xaml
	/// </summary>
	public partial class EditActivityPopup
	{
		public EditActivityPopup()
		{
			InitializeComponent();
		}

		void OnCloseButtonClicked( object sender, RoutedEventArgs e )
		{
			Close();
		}

		bool _overridingOccuranceChanged;
		void OnOccuranceChanged( object sender, RoutedPropertyChangedEventArgs<object> e )
		{
			// TODO: Limiting possible values can possibly be done better through coerce or something?

			if ( _overridingOccuranceChanged )
			{
				_overridingOccuranceChanged = false;
				return;
			}

			if ( e.OldValue == null )
			{
				return;
			}

			var oldTime = (DateTime)e.OldValue;
			var newTime = (DateTime)e.NewValue;

			// Only allow steps of 15 minutes when minutes are changed.
			TimeSpan diff = newTime - oldTime;
			if ( Math.Abs( diff.TotalMinutes ) < 2 )	// Changing minutes.
			{
				oldTime = oldTime.Round( DateTimePart.Minute );
				newTime = diff.TotalMinutes < 0
					? oldTime.SafeSubtract( TimeSpan.FromMinutes( 15 ) )
					: oldTime.SafeAdd( TimeSpan.FromMinutes( 15 ) );
			}

			// Prevent the time from being set in the past.
			_overridingOccuranceChanged = true;
			OccurancePicker.Value = newTime < DateTime.Now ? oldTime : newTime;
		}
	}
}
