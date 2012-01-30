using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Laevo.ViewModel.Main.Binding;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using Whathecode.System.Windows.Input.InputController;
using Whathecode.System.Windows.Input.InputController.Condition;
using Whathecode.System.Windows.Input.InputController.Trigger;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;


namespace Laevo.View.Main
{
	/// <summary>
	/// Interaction logic for TrayIconControl.xaml
	/// </summary>
	public partial class TrayIconControl
	{
		const int UpdatesPerSecond = 25;

		readonly Timer _updateLoop = new Timer();
		readonly KeyboardHookListener _keyboardListener = new KeyboardHookListener( new GlobalHooker() );
		readonly InputController _inputController = new InputController();
		readonly Dictionary<Keys, bool> _keyStates = new Dictionary<Keys, bool>();


		public TrayIconControl()
		{
			InitializeComponent();

			// Capture system-wide keyboard events.
			_keyboardListener.Enabled = true;
			_keyboardListener.KeyDown += OnKeyDown;
			_keyboardListener.KeyUp += OnKeyUp;
			_updateLoop.Interval = 1000 / UpdatesPerSecond;
			_updateLoop.Tick += OnUpdate;
			_updateLoop.Start();

			// Add triggers for desired system-wide commands.
			_keyStates[ Keys.CapsLock ] = false;
			KeyInputCondition capsLockUp = new KeyInputCondition( () => _keyStates[ Keys.CapsLock ], KeyInputCondition.KeyState.Up );
			_inputController.AddTrigger( new CommandBindingTrigger<Commands>( capsLockUp, this, Commands.OpenTimeLine ) );
		}

		~TrayIconControl()
		{
			_keyboardListener.KeyDown -= OnKeyDown;
			_keyboardListener.KeyUp -= OnKeyUp;
			_updateLoop.Tick -= OnUpdate;
			_updateLoop.Stop();
		}


		void OnKeyDown( object sender, KeyEventArgs e )
		{
			_keyStates[ e.KeyCode ] = true;

			// Disable caps lock.
			if ( e.KeyCode == Keys.CapsLock )
			{
				e.Handled = true;
			}
		}

		void OnKeyUp( object sender, KeyEventArgs e )
		{
			_keyStates[ e.KeyCode ] = false;
		}

		void OnUpdate( object sender, EventArgs e )
		{
			_inputController.Update();
		}
	}
}
