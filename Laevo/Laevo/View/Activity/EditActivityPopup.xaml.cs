using System;
using System.Windows;
using Whathecode.System.Extensions;
using Xceed.Wpf.Toolkit;


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

		bool _overridingDateTimePickerChanged;
		void OnDateTimePickerChanged( object sender, RoutedPropertyChangedEventArgs<object> e )
		{
			// TODO: Limiting possible values can possibly be done better through coerce or something?

			var picker = (DateTimePicker)sender;
			if ( _overridingDateTimePickerChanged )
			{
				_overridingDateTimePickerChanged = false;
				return;
			}

			if ( e.OldValue == null )
			{
				return;
			}

			var oldTime = (DateTime)e.OldValue;
			var newTime = (DateTime)e.NewValue;

			// Only allow steps of 15 minutes when minutes are changed.
			int diffMinutes = (int)(newTime - oldTime).TotalMinutes;
			// TODO: This is a hackish check to see whether minutes are changed. What is really needed is an event to see whether the up or down button is pressed.
			if ( Math.Abs( diffMinutes ) == 1 )
			{
				if ( diffMinutes > 0 )
				{
					newTime = oldTime.SafeAdd( TimeSpan.FromMinutes( Model.Laevo.SnapToMinutes ) );
				}
				newTime = Model.Laevo.GetNearestTime( newTime );
			}

			// Prevent the time from being set in the past.
			DateTime newValue = newTime < DateTime.Now ? oldTime : newTime;
			if ( picker.Value != newValue )
			{
				_overridingDateTimePickerChanged = true;
				picker.Value = newValue;
			}
		}

	}
}
