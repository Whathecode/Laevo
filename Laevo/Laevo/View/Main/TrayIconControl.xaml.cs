using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
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
using ActivityCommands = Laevo.ViewModel.Activity.Binding.Commands;


namespace Laevo.View.Main
{
	/// <summary>
	/// Interaction logic for TrayIconControl.xaml
	/// </summary>
	partial class TrayIconControl : IDisposable
	{
		const int UpdatesPerSecond = 25;

		MainViewModel _viewModel;
		readonly Timer _updateLoop = new Timer();
		KeyboardHookListener _keyboardListener;

		readonly Dispatcher _dispatcher;
		readonly InputController _inputController = new InputController();
		readonly Dictionary<Keys, bool> _keyStates = new Dictionary<Keys, bool>();
		readonly object _inputLock = new object();

		const string TurnCapsLockText = "Turn Caps Lock ";
		bool _isCapsLockEnabled;
		readonly object _switchingCapsLockLock = new object();


		public TrayIconControl( MainViewModel viewModel )
		{
			_viewModel = viewModel;
			_dispatcher = Dispatcher.CurrentDispatcher;

			viewModel.GuiReset += () =>
			{
				lock ( _inputLock )
				{
					ResetKeyStates();
				}
			};

			InitializeComponent();

			// Capture system-wide keyboard events.
			_isCapsLockEnabled = KeyHelper.IsCapsLockEnabled();
			InitializeKeyboardListener();
			_updateLoop.Interval = 1000 / UpdatesPerSecond;
			_updateLoop.Elapsed += ( s, a ) =>
			{
				// Use a dispatcher, otherwise race conditions might occur when 'OnUpdate' dispatches to the main thread.
				try
				{
					DispatcherHelper.SafeDispatch( _dispatcher, () => OnUpdate( s, a ) );
				}
				catch ( TaskCanceledException )
				{
					// This only happens on shutdown, which is nothing critical.
				}
			};
			_updateLoop.Start();

			UpdateCapsLockState();

			// Add triggers for desired system-wide commands.
			ResetKeyStates();
			var capsLockPressed = new KeyInputCondition( () => _keyStates[ Keys.CapsLock ], KeyInputCondition.KeyState.Pressed );
			AddExclusiveKeysTrigger( capsLockPressed, Keys.CapsLock, Commands.ShowActivityBar, false );
			var hideActivityBar = new KeyInputCondition( () => _keyStates[ Keys.CapsLock ], KeyInputCondition.KeyState.Up );
			AddExclusiveKeysTrigger( hideActivityBar, Keys.CapsLock, Commands.HideActivityBar );
			var switchOverview = new KeyInputCondition( () => _keyStates[ Keys.CapsLock ], KeyInputCondition.KeyState.Up );
			AddExclusiveKeysTrigger( switchOverview, Keys.CapsLock, Commands.SwitchActivityOverview );
			var newActivity = new AndCondition( capsLockPressed, new KeyInputCondition( () => _keyStates[ Keys.N ], KeyInputCondition.KeyState.Down ) );
			AddExclusiveKeysTrigger( newActivity, Keys.CapsLock | Keys.N, Commands.NewActivity );
			var cutWindow = new AndCondition( capsLockPressed, new KeyInputCondition( () => _keyStates[ Keys.X ], KeyInputCondition.KeyState.Down ) );
			AddExclusiveKeysTrigger( cutWindow, Keys.CapsLock | Keys.X, Commands.CutWindow );
			var pasteWindows = new AndCondition( capsLockPressed, new KeyInputCondition( () => _keyStates[ Keys.V ], KeyInputCondition.KeyState.Down ) );
			AddExclusiveKeysTrigger( pasteWindows, Keys.CapsLock | Keys.V, Commands.PasteWindows );
			var switchCapsLock = new AndCondition( capsLockPressed, new KeyInputCondition( () => _keyStates[ Keys.A ], KeyInputCondition.KeyState.Down ) );
			AddExclusiveKeysTrigger( switchCapsLock, Keys.CapsLock | Keys.A, SwitchCapsLock );
			var switchActivity = new AndCondition( capsLockPressed, new KeyInputCondition( () => _keyStates[ Keys.Tab ], KeyInputCondition.KeyState.Down ) );
			AddExclusiveKeysTrigger( switchActivity, Keys.CapsLock | Keys.Tab, Commands.SwitchActivity );
			var activateSelected = new SequentialCondition(
				new AndCondition( capsLockPressed, new KeyInputCondition( () => _keyStates[ Keys.Tab ], KeyInputCondition.KeyState.Down ) ),
				new KeyInputCondition( () => _keyStates[ Keys.CapsLock ], KeyInputCondition.KeyState.Up ) );
			AddExclusiveKeysTrigger( activateSelected, Keys.CapsLock | Keys.Tab, Commands.ActivateSelectedActivity );

			// Add triggers for activity commands.
			var stopActivity = new AndCondition( capsLockPressed, new KeyInputCondition( () => _keyStates[ Keys.W ], KeyInputCondition.KeyState.Down ) );
			AddExclusiveKeysTrigger( stopActivity, Keys.CapsLock | Keys.W, ActivityCommands.StopActivity );
			var openLibrary = new AndCondition(	capsLockPressed, new KeyInputCondition( () => _keyStates[ Keys.L ], KeyInputCondition.KeyState.Down ) );
			AddExclusiveKeysTrigger( openLibrary, Keys.CapsLock | Keys.L, ActivityCommands.OpenActivityLibrary );

			// Add trigger which resets the exclusive key triggers when keys are no longer pressed.
			var anyKeyDown = new DelegateCondition( () => _keyStates.All( s => !s.Value ) );
			var resetExclusiveTriggers = new EventTrigger( anyKeyDown );
			resetExclusiveTriggers.ConditionsMet += () => _exclusiveConditions.ForEach( c => c.Reset() );
			_inputController.AddTrigger( resetExclusiveTriggers );
		}


		void ResetKeyStates()
		{
			_keyStates.Clear();

			// Prevent exception when looking up a non-existent key.
			new[] { Keys.CapsLock, Keys.N, Keys.W, Keys.L, Keys.X, Keys.V, Keys.A, Keys.Tab }.ForEach( k => _keyStates[ k ] = false );

			// Set keystate of all keys which are currently down to true.
			List<Key> downKeys = KeyHelper.GetNonToggleKeysState();
			downKeys.ForEach( k => _keyStates[ (Keys)KeyInterop.VirtualKeyFromKey( k ) ] = true );
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

		void AddExclusiveKeysTrigger( AbstractCondition condition, Keys exclusiveKeys, ViewModel.Activity.Binding.Commands command, object parameter = null )
		{
			CreateExclusiveTrigger(
				condition, exclusiveKeys,
				c => new DynamicCommandBindingTrigger<ViewModel.Activity.Binding.Commands>(
					c,
					() => _viewModel.GetCurrentActivity(),
					command, parameter ) );
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

		readonly object _updateLock = new object();
		void OnUpdate( object sender, EventArgs e )
		{
			// Prevent locking up the system during heavy load and update events keep piling up.
			if ( !Monitor.TryEnter( _updateLock ) )
			{
				return;
			}

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

			lock ( _inputLock )
			{
				foreach ( var input in _newInput )
				{
					_keyStates[ input.Key ] = input.Value;
				}
				_newInput.Clear();

				_inputController.Update();
			}

			Monitor.Exit( _updateLock );
		}

		bool _suppressKeys;
		readonly Dictionary<Keys, bool> _newInput = new Dictionary<Keys, bool>();

		void OnKeyDown( object sender, KeyEventArgs e )
		{
			lock ( _inputLock )
			{
				// Ignore invalid events. (These Keys.None events do occur, but I'm not quite sure why: http://globalmousekeyhook.codeplex.com/workitem/1001)
				if ( e.KeyCode == Keys.None )
				{
					return;
				}

				_newInput[ e.KeyCode ] = true;

				// Disable caps lock, and any keys pressed simultaneously with caps lock.
				if ( e.KeyCode == Keys.CapsLock )
				{
					// HACK: Sometimes keys end up staying 'up' in _keyStates for a reason currently unknown.
					//       Refreshing this buffer whenever a shortkey will be used solves this for this particular application.
					ResetKeyStates();

					_suppressKeys = true;
				}
				if ( _suppressKeys )
				{
					e.Handled = true;
				}
			}
		}

		void OnKeyUp( object sender, KeyEventArgs e )
		{
			lock ( _inputLock )
			{
				// Ignore invalid events. (These Keys.None events do occur, but I'm not quite sure why: http://globalmousekeyhook.codeplex.com/workitem/1001)
				if ( e.KeyCode == Keys.None )
				{
					return;
				}

				_newInput[ e.KeyCode ] = false;

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