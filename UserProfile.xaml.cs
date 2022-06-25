using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace TouhouButtonWPF
{
	/// <summary>
	/// Interação lógica para UserProfile.xam
	/// </summary>
	public partial class UserProfile : UserControl
	{
		public const string NOTEPAD_PLACEHOLDER = "This is your personal notepad. Feel free to write whatever you want in it...";
		public const string NOTEPAD_PLACEHOLDER_OTHERS = "(This user hasn't written anything in their notepad yet...)";
		
		private readonly TouhouLauncher launcher;

		public User User { get; }
		public Action<string> OnShowLogIn { get; }
		public bool LoggedIn { get; set; } = false;

		// public UserProfile(string userName) : this(userName, name => { }) { }
		// ^^^ not really very needed

		public UserProfile(TouhouLauncher launcher, User user, Action<string> onShowLogIn)
		{
			InitializeComponent();
			User = user;
			Name.Content = user.Name;
			Notepad.Document.Blocks.Clear();
			Notepad.Document.Blocks.Add(new Paragraph(new Run(NOTEPAD_PLACEHOLDER)));
			UpdateHighScoreLabel();
			OnShowLogIn = onShowLogIn;
		
			this.launcher = launcher;
		}

		private void RichTextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			RichTextBox textBox = (RichTextBox)sender;
			var text = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd).Text.TrimEnd('\n');
			
			if (text.Contains(NOTEPAD_PLACEHOLDER)) Notepad.Document.Blocks.Clear();
		}

		private void RichTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			RichTextBox textBox = (RichTextBox)sender;
			var text = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd).Text;
			if (string.IsNullOrWhiteSpace(text))
			{
				Notepad.Document.Blocks.Clear();
				Notepad.Document.Blocks.Add(new Paragraph(new Run(NOTEPAD_PLACEHOLDER)));
			}
		}

		// Called when this profile's User has just signed in
		internal void LogIn()
		{
			LoggedIn = true;

			var text = new TextRange(Notepad.Document.ContentStart, Notepad.Document.ContentEnd).Text;
			if (text.Contains(NOTEPAD_PLACEHOLDER_OTHERS))
			{
				Notepad.Document.Blocks.Clear();
				Notepad.Document.Blocks.Add(new Paragraph(new Run(NOTEPAD_PLACEHOLDER)));
			}

			Notepad.IsReadOnly = false;
			Notepad.IsHitTestVisible = Notepad.Focusable = true;
			BorderBrush = new SolidColorBrush(Colors.LightPink);
		}

		// Called when any user has logged in
		internal void OnLoggedIn()
		{
			LogInPrompt.IsEnabled = false;
			LogInPrompt.Visibility = Visibility.Hidden;
		}

		public void UpdateHighScoreLabel() => HighScoreLabel.Content = $"{User.HighScore} points";

		private void LogInPrompt_MouseDown(object sender, MouseButtonEventArgs e) => OnShowLogIn(User.Name);

		private void UserControl_MouseEnter(object sender, MouseEventArgs e)
		{
			if (launcher.currentUser == null) LogInPrompt.IsEnabled = true;
		}

		private void UserControl_MouseLeave(object sender, MouseEventArgs e)
		{
			if (launcher.currentUser == null) LogInPrompt.IsEnabled = false;
		}
	}
}
