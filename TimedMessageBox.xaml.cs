using System;
using System.Media;
using System.Windows;
using System.Windows.Threading;

namespace TouhouButtonWPF
{
	/// <summary>
	/// Lógica interna para TimedMessageBox.xaml
	/// </summary>
	public partial class TimedMessageBox : Window
	{
		DispatcherTimer _timer;

		public int TimeLeft { get; set; }

		public TimedMessageBox(int timer)
		{
			InitializeComponent();
			SystemSounds.Beep.Play();

			//second time show error solved
			/*if (Application.Current == null) new Application();
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;*/

			TimeLeft = timer;

			void RefreshTimer()
			{
				if (TimeLeft <= 0)
				{
					OK.Content = $"OK";
					OK.IsEnabled = true;
					_timer.Stop();
				}
				else OK.Content = $"OK ({TimeLeft}s)";
				TimeLeft--;
			}

			_timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, (sender, e) => RefreshTimer(), Application.Current.Dispatcher);
			_timer.Start();
			RefreshTimer();
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e) => DialogResult = true;

		private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e) => MouseLeftButtonDown += delegate { DragMove(); };

		/// <summary>
		/// Displays a message box that has a message and that returns a result.
		/// May have a timer before the message can be dismissed.
		/// </summary>
		/// <param name="text">A System.String that specifies the text to display.</param>
		/// <param name="title">A System.String that specifies the title to display for the message box.</param>
		/// <param name="timer">An int that represents the amount, in seconds, that the user must wait before dismissing the message box.</param>
		/// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
		/// 
		public static bool? Show(string text, string title = "", int timer = 0)
		{
			TimedMessageBox msg = new(timer)
			{
				TitleBar = { Text = title },
				Textbar = { Text = text },
			};
			return msg.ShowDialog();
		}
	}
}
