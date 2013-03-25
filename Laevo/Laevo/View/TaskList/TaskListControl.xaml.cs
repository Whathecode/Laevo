using System.Windows;
using System.Windows.Input;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.DependencyPropertyFactory.Aspects;
using Whathecode.System.Windows.DependencyPropertyFactory.Attributes;


namespace Laevo.View.TaskList
{
	/// <summary>
	///   Interaction logic for TaskListControl.xaml
	/// </summary>
	[WpfControl( typeof( Properties ) )]
	partial class TaskListControl
	{
		public enum Properties
		{
			TaskHasFocus
		}


		[DependencyProperty( Properties.TaskHasFocus )]
		public bool TaskHasFocus { get; private set; }


		public TaskListControl()
		{
			InitializeComponent();
		}


		void OnTaskNameFocusChanged( object sender, DependencyPropertyChangedEventArgs e )
		{
			TaskHasFocus = (bool)e.NewValue;
		}

		void TaskNameKeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key.EqualsAny( Key.Enter, Key.Escape ) )
			{
				Common.ForceUpdateSource( e );
				e.Handled = true;
			}
		}
	}
}
