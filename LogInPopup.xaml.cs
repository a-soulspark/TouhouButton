using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TouhouButtonWPF
{
	/// <summary>
	/// Lógica interna para LogInPopup.xaml
	/// </summary>
	public partial class LogInPopup : Window
	{
		public Action<User> OnLogIn { get; }
		public Action OnCancelled { get; }
		public bool RegisterMode { get; set; } = false;

		private bool _loggedIn = false;

		public LogInPopup(Action<User> onLogIn, bool registerMode, string defaultUserName = "")
		{
			InitializeComponent();
			OnLogIn = onLogIn;
			OnCancelled = () => { };
			UserNameField.Text = defaultUserName;
			RegisterMode = registerMode;

			if (defaultUserName != "") PassField.Focus();
			LogInButton.Content = WindowTitle.Content = RegisterMode ? "Register" : "Log In";
		}

		/*
		public static class CustomCommands
		{
			public static readonly RoutedUICommand Exit = new RoutedUICommand("Submit Password", "SubmitPassword", typeof(CustomCommands)*//*,
				new InputGestureCollection() { new KeyGesture(Key.Enter) }*//*);

			//Define more commands here, just like the one above
		}*/

		private void SubmitPassword()
		{
			// This code is just a bunch of Guard clauses. It's kinda hideous... but it works really well.
			// (it's even said to be "good practice" to do this 🤔)

			if (string.IsNullOrWhiteSpace(UserNameField.Text))
			{
				MessageBox.Show("The user name field cannot be empty.");
				return;
			}

			TouhouLauncher.Users.TryGetValue(UserNameField.Text, out var user);

			if (RegisterMode && user != null)
			{
				MessageBox.Show($"The user \"{UserNameField.Text}\" already exists.");
				return;
			}

			if (!RegisterMode && user == null)
			{
				MessageBox.Show($"The user \"{UserNameField.Text}\" does not exist.");
				return;
			}
				
			if (!RegisterMode && PassField.Password.GetHashCode() != user.PasswordHash)
			{
				MessageBox.Show($"The user name or password do not match.");
				return;
			}

			// this condition could be simpler
			// ( || user == null) ----> unnecessary
			// but otherwise VS keeps naggin me about this
			if (RegisterMode || user == null) user = new User(UserNameField.Text, PassField.Password.GetHashCode());
			
			Close();
			OnLogIn(user);
			MessageBox.Show($"Welcome, {UserNameField.Text}!");
			/*
			Notepad.IsReadOnly = false;
			Notepad.Focusable = true;
			PassSubmit.IsEnabled = false;
			PassField.IsEnabled = false;
			PassField.Password = "";
			LogInTimer.Visibility = Visibility.Visible;

			Timer timer = new(1 * 60 * 1000) { AutoReset = false };
			timer.Elapsed += (sender, e) => Dispatcher.Invoke(() =>
			{
				Notepad.IsReadOnly = true;
				PassSubmit.IsEnabled = true;
				PassField.IsEnabled = true;
				Notepad.Focusable = false;
				LogInTimer.Visibility = Visibility.Hidden;
			});
			*//*
							TIME TO SAVE SOME DATA!!!!!
								+ rememeber, maybe order by last modified...*//*

			timer.Start();*/
		}

		private void PassField_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) SubmitPassword();
		}

		private void LogInButton_Click(object sender, RoutedEventArgs e) => SubmitPassword();

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!_loggedIn) OnCancelled();
		}

		private void TitleBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

		private void Close_Click(object sender, RoutedEventArgs e) => Close();
	}
}
