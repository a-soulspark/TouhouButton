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

		public string UserName { get; }
		public Action<string> OnShowLogIn { get; }

		//public UserProfile(string userName) : this(userName, name => { }) { }
		// ^^^ not really very needed

		public UserProfile(string userName, Action<string> onShowLogIn)
		{
			InitializeComponent();
			Name.Content = userName;
			Notepad.Document.Blocks.Clear();
			Notepad.Document.Blocks.Add(new Paragraph(new Run(NOTEPAD_PLACEHOLDER)));
			UserName = userName;
			OnShowLogIn = onShowLogIn;
		}

		private void RichTextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			RichTextBox textBox = (RichTextBox)sender;
			var text = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd).Text.TrimEnd('\n');
			Debug.WriteLine($"{text}\n\n\n");
			if (text.Contains(NOTEPAD_PLACEHOLDER))
			{
				SystemSounds.Beep.Play();
				Notepad.Document.Blocks.Clear();
			}
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

		private void Label_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => OnShowLogIn(UserName);
	}
}
