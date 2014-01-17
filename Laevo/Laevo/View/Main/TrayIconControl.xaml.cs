using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using Laevo.ViewModel.Main;
using Laevo.ViewModel.Main.Binding;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using Whathecode.System.Extensions;
using Whathecode.System.Windows.Input;
using Whathecode.System.Windows.Input.InputController;
using Whathecode.System.Windows.Input.InputController.Condition;
using Whathecode.System.Windows.Input.InputController.Trigger;
using Whathecode.System.Windows.Threading;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using Timer = System.Timers.Timer;


namespace Laevo.View.Main
{
	/// <summary>
	/// Interaction logic for TrayIconControl.xaml
	/// </summary>
	partial class TrayIconControl : IDisposable
	{
		const int UpdatesPerSecond = 25;

		readonly Timer _updateLoop = new Timer();
		KeyboardHookListener _keyboardListener;

		readonly InputController _inputController = new InputController();
		readonly Dictionary<Keys, bool> _keyStates = new Dictionary<Keys, bool>();

		const string TurnCapsLockText = "Turn Caps Lock ";
		bool _isCapsLockEnabled;
		readonly object _switchingCapsLockLock = new object();


		public TrayIconControl( MainViewModel viewModel )
		{
			viewModel.GuiReset += ResetKeyStates;

			InitializeComponent();

			// Capture system-wide keyboard events.
			_isCapsLockEnabled = KeyHelper.IsCapsLockEnabled();
			InitializeKeyboardListener();
			_updateLoop.Interval = 1000 / UpdatesPerSecond;
			_updateLoop.Elapsed += OnUpdate;
			_updateLoop.Start();

			UpdateCapsLockState();

			// Add triggers for desired system-wide commands.
			ResetKeyStates();
			var capsLockPressed = new KeyInputCondition( () => _keyStates[ Keys.CapsLock ], KeyInputCondition.KeyState.Pressed );
			AddExclusiveKeysTrigger( capsLockPressed, Keys.CapsLock, Commands.ShowActivityBar, false );
			var switchOverview = new KeyInputCondition( () => _keyStates[ Keys.CapsLock ], KeyInputCondition.KeyState.Up );
			AddExclusiveKeysTrigger( switchOverview, Keys.CapsLock, Commands.SwitchActivityOverview );
			var newActivity = new KeyInputCondition( () => _keyStates[ Keys.N ], KeyInputCondition.KeyState.Down );
			AddExclusiveKeysTrigger( new AndCondition( capsLockPressed, newActivity ), Keys.CapsLock | Keys.N, Commands.NewActivity );
			var stopActivity = new KeyInputCondition( () => _keyStates[ Keys.W ], KeyInputCondition.KeyState.Down );
			AddExclusiveKeysTrigger( new AndCondition( capsLockPressed, stopActivity ), Keys.CapsLock | Keys.W, Commands.StopActivity );
			var openLibrary = new KeyInputCondition( () => _keyStates[ Keys.L ], KeyInputCondition.KeyState.Down );
			AddExclusiveKeysTrigger( new AndCondition( capsLockPressed, openLibrary ), Keys.CapsLock | Keys.L, Commands.OpenCurrentActivityLibrary );
			var cutWindow = new KeyInputCondition( () => _keyStates[ Keys.X ], KeyInputCondition.KeyState.Down );
			AddExclusiveKeysTrigger( new AndCondition( capsLockPressed, cutWindow ), Keys.CapsLock | Keys.X, Commands.CutWindow );
			var pasteWindows = new KeyInputCondition( () => _keyStates[ Keys.V ], KeyInputCondition.KeyState.Down );
			AddExclusiveKeysTrigger( new AndCondition( capsLockPressed, pasteWindows ), Keys.CapsLock | Keys.V, Commands.PasteWindows );
			var switchCapsLock = new KeyInputCondition( () => _keyStates[ Keys.A ], KeyInputCondition.KeyState.Down );
			AddExclusiveKeysTrigger( new AndCondition( capsLockPressed, switchCapsLock ), Keys.CapsLock | Keys.A, SwitchCapsLock );
			var switchActivity = new KeyInputCondition( () => _keyStates[ Keys.Tab ], KeyInputCondition.KeyState.Down );
			AddExclusiveKeysTrigger( new AndCondition( capsLockPressed, switchActivity ), Keys.CapsLock | Keys.Tab, Commands.SwitchActivity );
			var activateSelectedActivity = new KeyInputCondition( () => _keyStates[ Keys.CapsLock ], KeyInputCondition.KeyState.Released );
			AddExclusiveKeysTrigger( activateSelectedActivity, Keys.CapsLock, Commands.ActivateSelectedActivity );

			// Add trigger which resets the exclusive key triggers when keys are no longer pressed.
			var anyKeyDown = new DelegateCondition( () => _keyStates.All( s => !s.Value ) );
			var resetExclusiveTriggers = new EventTrigger( anyKeyDown );
			resetExclusiveTriggers.ConditionsMet += () => _exclusiveConditions.ForEach( c => c.Reset() );
			_inputController.AddTrigger( resetExclusiveTriggers );
		}


		bool _resettingKeyStates;
		void ResetKeyStates()
		{
			lock ( _inputController )
			{
				// Throttle resetting key states, since this is a slow operation.
				// When called too often (repeatedly pressing caps lock), it locks up the system.
				if ( _resettingKeyStates )
				{
					return;
				}
				_resettingKeyStates = true;

				_keyStates.Clear();

				// Prevent exception when looking up a non-existent key.
				new[] { Keys.CapsLock, Keys.N, Keys.W, Keys.L, Keys.X, Keys.V, Keys.A, Keys.Tab }.ForEach( k => _keyStates[ k ] = false );

				// Set keystate of all keys which are currently down to true.
				List<Key> downKeys = KeyHelper.GetNonToggleKeysState();
				downKeys.ForEach( k => _keyStates[ (Keys)KeyInterop.VirtualKeyFromKey( k ) ] = true );

				_resettingKeyStates = false;
			}
		}

		void InitializeKeyboardListener()
		{
			SafeKeyboardHookListenerDispose();

			_keyboardListener = new KeyboardHookListener( new GlobalHooker() );
			_keyboardListener.KeyDown += OnKeyDown;
			_keyboardListener.KeyUp += OnKeyUp;
			_keyboardListener.Start();
		}

		void SafeKeyboardHookListenerDispose()
		{
			if ( _keyboardListener == null )
			{
				return;
			}

			try
			{
				_keyboardListener.Dispose();
			}
			catch ( Win32Exception )
			{
				// HACK: Sometimes the internal hook of the KeyboardHookListener is no longer valid (unhooked?).
				//       This might be due to a bug in the library used: http://globalmousekeyhook.codeplex.com/workitem/929
			}
		}

		readonly List<ExclusiveCondition> _exclusiveConditions = new List<ExclusiveCondition>();

		void AddExclusiveKeysTrigger( AbstractCondition condition, Keys exclusiveKeys, Commands command, object parameter = null )
		{
			CreateExclusiveTrigger(
				condition, exclusiveKeys,
				c => new CommandBindingTrigger<Commands>( c, this, command, parameter ) );
		}

		void AddExclusiveKeysTrigger( AbstractCondition condition, Keys exclusiveKeys, Action action )
		{
			var dispatcher = Dispatcher;
			CreateExclusiveTrigger(
				condition, exclusiveKeys,
				c =>
				{
					var trigger = new EventTrigger( c );
					trigger.ConditionsMet += () => dispatcher.Invoke( action );
					return trigger;
				} );
		}

		void CreateExclusiveTrigger( AbstractCondition condition, Keys exclusiveKeys, Func<AbstractCondition, EventTrigger> createTrigger )
		{
			Func<bool> otherThanExclusive = () => _keyStates.Any( k => !exclusiveKeys.HasFlag( k.Key ) && k.Value );
			var exclusiveCondition = new ExclusiveCondition( condition, new DelegateCondition( otherThanExclusive ) );
			EventTrigger trigger = createTrigger( exclusiveCondition );
			_inputController.AddTrigger( trigger );
			_exclusiveConditions.Add( exclusiveCondition );
		}

		bool _isDisposed;

		~TrayIconControl()
		{
			Dispose( false );
		}

		public void Dispose()
		{
			Dispose( true );
		}

		public void Dispose( bool isDisposing )
		{
			if ( _isDisposed )
			{
				return;
			}

			_updateLoop.Elapsed -= OnUpdate;
			SafeKeyboardHookListenerDispose();
			_updateLoop.Stop();
			Icon.Dispose();

			_isDisposed = true;
		}


		void OnUpdate( object sender, EventArgs e )
		{
			// HACK: Verify whether caps lock was enabled/disabled without SwitchCapsLock being called. This indicates the global keyboard hook was silently removed.
			//       http://msdn.microsoft.com/en-us/library/windows/desktop/ms646293(v=vs.85).aspx
			bool keyboardHookLost = false;
			lock ( _switchingCapsLockLock )
			{
				bool isCapsEnabled = KeyHelper.IsCapsLockEnabled();
				if ( isCapsEnabled != _isCapsLockEnabled )
				{
					keyboardHookLost = true;
					_isCapsLockEnabled = isCapsEnabled;
				}
			}

			if ( keyboardHookLost )
			{
				Dispatcher.Invoke( InitializeKeyboardListener ); // Dispatcher needs to be used since this is executed from the timer thread.
				SwitchCapsLock(); // Reset caps lock to its previous position.
			}

			lock ( _inputController )
			{
				lock ( _newInput )
				{
					foreach ( var input in _newInput )
					{
						_keyStates[ input.Key ] = input.Value;
					}
					_newInput.Clear();
				}

				_inputController.Update();
			}
		}

		bool _suppressKeys;
		readonly Dictionary<Keys, bool> _newInput = new Dictionary<Keys, bool>();

		void OnKeyDown( object sender, KeyEventArgs e )
		{
			// Ignore invalid events. (These Keys.None events do occur, but I'm not quite sure why: http://globalmousekeyhook.codeplex.com/workitem/1001)
			if ( e.KeyCode == Keys.None )
			{
				return;
			}

			lock ( _newInput )
			{
				_newInput[ e.KeyCode ] = true;
			}

			// Disable caps lock, and any keys pressed simultaneously with caps lock.
			if ( e.KeyCode == Keys.CapsLock )
			{
				_suppressKeys = true;
			}
			if ( _suppressKeys )
			{
				e.Handled = true;
			}
		}

		void OnKeyUp( object sender, KeyEventArgs e )
		{
			// Ignore invalid events. (These Keys.None events do occur, but I'm not quite sure why: http://globalmousekeyhook.codeplex.com/workitem/1001)
			if ( e.KeyCode == Keys.None )
			{
				return;
			}

			lock ( _newInput )
			{
				_newInput[ e.KeyCode ] = false;
			}

			// Re-enable key input when Caps Lock is released.
			if ( e.KeyCode == Keys.CapsLock )
			{
				_suppressKeys = false;
			}
			if ( _suppressKeys )
			{
				e.Handled = true;
			}
		}

		void SwitchCapsLock( object sender, System.Windows.RoutedEventArgs e )
		{
			SwitchCapsLock();
		}

		void SwitchCapsLock()
		{
			lock ( _switchingCapsLockLock )
			{
				_keyboardListener.KeyDown -= OnKeyDown;
				_keyboardListener.KeyUp -= OnKeyUp;
				KeyHelper.SimulateKeyPress( Key.CapsLock );
				_keyboardListener.KeyDown += OnKeyDown;
				_keyboardListener.KeyUp += OnKeyUp;
				_isCapsLockEnabled = !_isCapsLockEnabled;
			}

			UpdateCapsLockState();
		}

		void UpdateCapsLockState()
		{
			string headerText = TurnCapsLockText + ( _isCapsLockEnabled ? "Off" : "On" );
			DispatcherHelper.SafeDispatch( CapsLockMenuItem.Dispatcher, () => CapsLockMenuItem.Header = headerText );
		}
	}
}