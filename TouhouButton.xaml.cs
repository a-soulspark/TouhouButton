using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TouhouButtonWPF
{
	public partial class TouhouButton : Window
	{
		// Hide from Alt-Tab
		[DllImport("user32.dll", SetLastError = true)]
		static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
		private const int GWL_EX_STYLE = -20;
		private const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;

		private bool locked = true;
		private bool shouldClose = false;
		public const string START_SOUND_PATH = "./resources/start.wav";

		public TouhouButton() => InitializeComponent();

		// SourceInitialized is the right moment to read configuration files (or so it seems)
		private void Window_SourceInitialized(object sender, EventArgs e) => ReadJsonConfig();

		// Reads the options specified in the config.json file
		private void ReadJsonConfig()
		{
			using var stream = File.Open(Directory.GetCurrentDirectory() + "/config.json", FileMode.Open);
			try
			{
				JsonNode? json = JsonNode.Parse(stream);
				if (json != null)
				{
					Debug.WriteLine($"Configuration loaded: {json}");

					// Load images from resources
					touhouButton.Source = new BitmapImage(Utils.GetUri("./resources/button.png"));
					title.Source = new BitmapImage(Utils.GetUri("./resources/title.png"));

					// Load window coordinates back
					JsonUtils.TryGetNode(json, "window", node =>
					{
						var windowJson = node.AsObject();
						JsonUtils.TryGetValue<double>(windowJson, "top", value => Top = value);
						JsonUtils.TryGetValue<double>(windowJson, "left", value => Left = value);
					});

					// Load window background gradient colors
					GradientBrush brush = (GradientBrush)background.Fill;
					JsonUtils.TryGetValue<string>(json, "backgroundColors", value =>
					{
						brush.GradientStops = GradientStopCollection.Parse(value);
						background.Fill = brush;
					});
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to load configuration file.\n{ex}");
			}
		}

		// supress Alt-F4
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!shouldClose)
			{
				e.Cancel = true;
				return;
			}
		}

		// Updates the config.json file with the new window coordinates
		private void SaveJsonConfig()
		{
			FileStream stream = File.Open(Directory.GetCurrentDirectory() + "/config.json", FileMode.Open);
			try
			{
				JsonNode? json = JsonNode.Parse(stream);
				if (json != null)
				{
					stream.Position = 0;
					Utf8JsonWriter writer = new(stream, new JsonWriterOptions() { Indented = true });
					var jsonObject = json.AsObject();

					jsonObject.Add("window", new JsonObject
					{
						new("top", JsonValue.Create(Top)),
						new("left", JsonValue.Create(Left))
					});

					jsonObject.WriteTo(writer);
					writer.Flush();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to write to configuration file.\n{ex}");
			}
		}

		// Button animations
		private void Window_MouseEnter(object sender, MouseEventArgs e) => ShiftTouhouButton(-1);
		private void Window_MouseLeave(object sender, MouseEventArgs e) => ShiftTouhouButton(1);

		// Window Draggable
		private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// If the button is locked, play Button animations
			// Otherwise, drag window around and save its position afterwards
			if (locked)
			{
				ShiftTouhouButton(-1);
				CaptureMouse();
			}
			else
			{
				DragMove();
				SaveJsonConfig();
			}
		}

		private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// If the button is locked, open the Launcher
			if (locked)
			{
				ShiftTouhouButton(1);
				ReleaseMouseCapture();
				// Since the mouse was captured when this event is triggered,
				// it must ensure that the mouse is actually above the TouhouButton
				if (IsMouseOver) OpenLauncher();
			}
		}

		// Opens the TouhouLauncher (wow)
		private void OpenLauncher()
		{
			using (var stream = File.Open(START_SOUND_PATH, FileMode.Open))
			{
				var soundPlayer = new SoundPlayer(stream);
				soundPlayer.Play();
			}

			/* Hides the TouhouButton, and stays hidden until
			 1. The TouhouLauncher opens up an instance of the game, and then waits until that instance then gets closed.
			 2. The TouhouLauncher is dismissed
			 --------------------------------------------
			 * TODO: Make this not so bad
			 * (I already added a TODO related to this one, about the Launcher hiding itself when the game opens instead of closing)
			 */
			Opacity = 0;
			new TouhouLauncher(process =>
			{
				process.WaitForExit();
				Opacity = 1;
			}, () => Opacity = 1).Show();
		}

		// Shifts the Touhou Button's image by 3*factor px.
		// Helps give more feedback to the user's actions.
		private void ShiftTouhouButton(int factor)
		{
			var margin = touhouButton.Margin;
			margin.Bottom += 3 * factor;
			touhouButton.Margin = margin;
		}

		// Window shortcuts
		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			// CTRL + SHIFT + L --> Toggling window locked/unlocked
			if (e.Key == Key.L && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
			{
				locked = !locked;
				MessageBox.Show(locked ? "Locked Touhou Button!" : "Unlocked Touhou Button!");
			}

			// CTRL + SHIFT + Q --> Force-Quitting (Alt-F4 doesn't work >:)
			if (e.Key == Key.Q && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
			{
				shouldClose = true;
				Close();
			}

			// CTRL + SHIFT + Q --> Reveal Database folder in the File Explorer
			if (e.Key == Key.D && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
				Process.Start("explorer.exe", @$"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\TouhouButton\database");
		}

		// When the window is loaded, make sure it behaves as intended (similar to a Windows Gadget)
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// Hide from the Alt-Tab menu
			var handle = new WindowInteropHelper(this).Handle;
			SetWindowLong(handle, GWL_EX_STYLE, (GetWindowLong(handle, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

			// Register Win+D avoidance
			ShowDesktop.AddHook(this);
		}
	}

	/* This class ensures that the window will always be visible, even when using the Win+D action.
	 * It does not work 100% of the times, due to how stupidly inconsistent Win+D is, but hey...
	 * That's on Microsoft, not me.

	 * Don't even attempt to understand this code, for your own sake.*/
	public static class ShowDesktop
	{
		[DllImport("user32.dll")]
		internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, ShowDesktop.WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

		[DllImport("user32.dll")]
		internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

		[DllImport("user32.dll")]
		internal static extern int GetClassName(IntPtr hwnd, StringBuilder name, int count);

		private const uint WINEVENT_OUTOFCONTEXT = 0u;
		private const uint EVENT_SYSTEM_FOREGROUND = 3u;

		public static void AddHook(Window window)
		{
			if (_hookIntPtr != null) return;

			_delegate = new WinEventDelegate(WinEventHook);
			_hookIntPtr = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _delegate, 0, 0, WINEVENT_OUTOFCONTEXT);
			_window = window;
		}

		private static string GetWindowClass(IntPtr hwnd)
		{
			StringBuilder _sb = new StringBuilder(32);
			GetClassName(hwnd, _sb, _sb.Capacity);
			return _sb.ToString();
		}

		internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

		private static void WinEventHook(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			string _class = GetWindowClass(hwnd);

			Action action = () => _window.Topmost = (string.Equals(_class, "WorkerW", StringComparison.Ordinal) || string.Equals(_class, "ProgMan", StringComparison.Ordinal));
			_window.Dispatcher.BeginInvoke(action);
		}

		private static Window? _window { get; set; }

		private static WinEventDelegate? _delegate { get; set; }

		private static IntPtr? _hookIntPtr { get; set; }
	}
}
