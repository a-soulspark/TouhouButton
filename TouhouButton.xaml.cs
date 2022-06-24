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
		private string soundPath = "start.wav";

		public TouhouButton()
		{
			InitializeComponent();
		}

		// supress Alt-F4
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!shouldClose)
			{
				e.Cancel = true;
				return;
			}

			using (FileStream stream = File.Open(Directory.GetCurrentDirectory() + "/config.json", FileMode.Open))
			{
				try
				{
					JsonNode? json = JsonNode.Parse(stream);
					if (json != null)
					{
						stream.Position = 0;
						Utf8JsonWriter writer = new(stream, new JsonWriterOptions() { Indented = true });
						var jsonObject = json.AsObject();

						var windowJson = new JsonObject();
						windowJson.Add(new("top", JsonValue.Create(Top)));
						windowJson.Add(new("left", JsonValue.Create(Left)));
						jsonObject.Add("window", windowJson);

						jsonObject.WriteTo(writer);
						writer.Flush();
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Failed to load configuration file.\n{ex}");
				}
			}
		}

		private void Window_MouseEnter(object sender, MouseEventArgs e) => ShiftTouhouButton(-1);
		private void Window_MouseLeave(object sender, MouseEventArgs e) => ShiftTouhouButton(1);

		private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (locked)
			{
				ShiftTouhouButton(-1);
				CaptureMouse();
			}
			else DragMove();
		}

		private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (locked)
			{
				ShiftTouhouButton(1);
				ReleaseMouseCapture();
				if (!IsMouseOver) return;

					using (var stream = File.Open(soundPath, FileMode.Open))
				{
					var soundPlayer = new SoundPlayer(stream);
					soundPlayer.Play();
				}

				Opacity = 0;
				TouhouLauncher touhouLauncher = new TouhouLauncher(process =>
				{
					process.WaitForExit();
					Opacity = 1;
				}, () => Opacity = 1);

				touhouLauncher.Show();
			}
		}

		private void ShiftTouhouButton(int factor)
		{
			var margin = touhouButton.Margin;
			margin.Bottom += 3 * factor;
			touhouButton.Margin = margin;
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.L && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
			{
				locked = !locked;
				MessageBox.Show(locked ? "Locked Touhou Button!" : "Unlocked Touhou Button!");
			}

			if (e.Key == Key.Q && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
			{
				shouldClose = true;
				Close();
			}
		}

		private void Window_SourceInitialized(object sender, EventArgs e)
		{
			using (FileStream stream = File.Open(Directory.GetCurrentDirectory() + "/config.json", FileMode.Open))
			{
				try
				{
					JsonNode? json = JsonNode.Parse(stream);
					if (json != null)
					{
						Debug.WriteLine($"Configuration loaded: {json}");

						// Store information
						JsonUtils.TryGetUri(json, "buttonPath", uri => touhouButton.Source = new BitmapImage(uri));
						JsonUtils.TryGetUri(json, "titlePath", uri => title.Source = new BitmapImage(uri));
						JsonUtils.TryGetValue<string>(json, "soundPath", value => soundPath = value);
						
						JsonUtils.TryGetNode(json, "window", node =>
						{
							var windowJson = node.AsObject();
							JsonUtils.TryGetValue<double>(windowJson, "top", value => Top = value);
							JsonUtils.TryGetValue<double>(windowJson, "left", value => Left = value);
						});

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
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var handle = new WindowInteropHelper(this).Handle;
			SetWindowLong(handle, GWL_EX_STYLE, (GetWindowLong(handle, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

			// Register Win+D avoidance
			ShowDesktop.AddHook(this);
		}
	}

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

		/*public static void RemoveHook()
		{
			if (_hookIntPtr == null) return;

			UnhookWinEvent(_hookIntPtr.Value);

			_delegate = null;
			_hookIntPtr = null;
			_window = null;
		}*/

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

			Action action = () =>
			{
				_window.Topmost = (string.Equals(_class, "WorkerW", StringComparison.Ordinal) || string.Equals(_class, "ProgMan", StringComparison.Ordinal));
			};
			_window.Dispatcher.BeginInvoke(action);
		}

		private static Window? _window { get; set; }

		private static WinEventDelegate? _delegate { get; set; }

		private static IntPtr? _hookIntPtr { get; set; }
	}
}
