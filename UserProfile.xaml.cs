using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TouhouButtonWPF
{
	/// <summary>
	/// Interação lógica para UserProfile.xam
	/// </summary>
	public partial class UserProfile : UserControl
	{
		private const string NOTEPAD_PLACEHOLDER = "This is your personal notepad. Feel free to write whatever you want in it...";

		public User User { get; }
		public Action<string> OnShowLogIn { get; }
		public bool LoggedIn { get; set; } = false;

		// public UserProfile(string userName) : this(userName, name => { }) { }
		// ^^^ not really very needed

		public UserProfile(User user, Action<string> onShowLogIn)
		{
			InitializeComponent();
			User = user;
			Name.Content = user.Name;
			Notepad.Document.Blocks.Clear();
			Notepad.Document.Blocks.Add(new Paragraph(new Run(NOTEPAD_PLACEHOLDER)));
			OnShowLogIn = onShowLogIn;
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

		internal void LogIn()
		{
			LoggedIn = true;

			LogInPrompt.IsEnabled = false;
			LogInPrompt.Visibility = Visibility.Hidden;
			Notepad.IsReadOnly = false;
			Notepad.IsHitTestVisible = Notepad.Focusable = true;
		}

		private void LogInPrompt_MouseUp(object sender, MouseButtonEventArgs e) => OnShowLogIn(User.Name);

		private void UserControl_MouseEnter(object sender, MouseEventArgs e)
		{
			if (!LoggedIn) LogInPrompt.IsEnabled = true;
		}

		private void UserControl_MouseLeave(object sender, MouseEventArgs e)
		{
			if (!LoggedIn) LogInPrompt.IsEnabled = false;
		}
	}
}
