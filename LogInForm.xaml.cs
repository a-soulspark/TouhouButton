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
using System.Security.Cryptography;

namespace TouhouButtonWPF
{
	/// <summary>
	/// Lógica interna para LogInPopup.xaml
	/// </summary>
	public partial class LogInForm : Window
	{
		public Action<User> OnLogIn { get; }
		public bool RegisterMode { get; set; } = false;

		private readonly TouhouLauncher launcher;

		public LogInForm(TouhouLauncher launcher, Action<User> onLogIn, bool registerMode, string defaultUserName = "")
		{
			InitializeComponent();
			this.launcher = launcher;
			OnLogIn = onLogIn;
			UserNameField.Text = defaultUserName;
			RegisterMode = registerMode;

			if (!RegisterMode) PassRepeatField.Visibility = PassRepeatLabel.Visibility = Visibility.Collapsed;

			if (defaultUserName != "") PassField.Focus();
			LogInButton.Content = WindowTitle.Content = RegisterMode ? "Register" : "Log In";
		}

		private void SubmitPassword()
		{
			// Looks through all the UserProfile controls,
			// searching for one whose name matches the UserNameField's text
			User? user = null;
			foreach (UserProfile userProfile in launcher.SocialStack.Children)
			{
				if (userProfile.User.Name == UserNameField.Text) user = userProfile.User;
			}

			/*---------------------------------------------------------------------------/
			   Read the TimedMessageBox texts to understand what each clause checks for
			/---------------------------------------------------------------------------*/

			if (string.IsNullOrWhiteSpace(UserNameField.Text))
			{
				TimedMessageBox.Show("The user name field cannot be empty.", "⚠️ Notice");
				UserNameField.Focus();
				return;
			}

			if (RegisterMode && user != null)
			{
				TimedMessageBox.Show($"The user \"{UserNameField.Text}\" already exists.", "⚠️ Notice");
				UserNameField.Focus();
				return;
			}

			if (!RegisterMode && user == null)
			{
				TimedMessageBox.Show($"The user \"{UserNameField.Text}\" does not exist.", "⚠️ Notice");
				UserNameField.Focus();
				return;
			}

			if (RegisterMode && PassField.Password.Length < 8)
			{
				TimedMessageBox.Show($"The password chosen is too weak.\nC'mon, you can do better than that! (8 chars. minimum)", "⚠️ Notice");
				PassField.Focus();
				return;
			}

			if (RegisterMode && PassField.Password != PassRepeatField.Password)
			{
				TimedMessageBox.Show($"The passwords in both password fields are not the same.", "⚠️ Notice");
				PassRepeatField.Focus();
				return;
			}

			// No longer using SHA256, replaced with the PBKDF2 algorithm (Rfc2898DeriveBytes). This code was replaced:
			// - using (var sha256Hash = SHA256.Create())
			// - GetHash(sha256Hash, PassField.Password)

			if (!RegisterMode)
			{
				// Equivalent to GetHash
				Rfc2898DeriveBytes k1 = new(PassField.Password, user.PasswordSalt, 2048);
				var key = Convert.ToBase64String(k1.GetBytes(16));

				// Check if the hash from the PassField's Password matches the user's PasswordHash
				if (!RegisterMode && key != user.PasswordHash)
				{
					TimedMessageBox.Show($"The user name or password do not match.", "⚠️ Notice", 1);
					PassField.Focus();
					return;
				}
			}
			else
			{
				// Equivalent to GetHash
				Rfc2898DeriveBytes k1 = new(PassField.Password, 32, 2048);
				var key = Convert.ToBase64String(k1.GetBytes(16));

				// Create new user!
				user = new User(UserNameField.Text, key, k1.Salt);
			}

			// Update login time to be the current Unix timestamp
			user.LastLoginTime = Utils.UnixTimestampNow();

			Close();
			OnLogIn(user);
			TimedMessageBox.Show($"Welcome, {UserNameField.Text}!", "✓ Logged In");
		}

		// TitleBar Draggable
		private void TitleBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

		// TitleBar Close button
		private void Close_Click(object sender, RoutedEventArgs e) => Close();

		// Regardless of Register Mode, the LogInButton can always be used to submit the password.
		private void LogInButton_Click(object sender, RoutedEventArgs e) => SubmitPassword();

		// If Register Mode is Off, the PassField is the field to submt the pass when hitting enter.
		private void PassField_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				// If Register Mode is Off, the PassRepeatField does not exist.
				// Thus, you can just submit the pass directly.
				if (!RegisterMode) SubmitPassword();
				else PassRepeatField.Focus();
			}
		}

		// If Register Mode is On, the PassRepeatField is the field to submt the pass when hitting enter.
		private void PassRepeatField_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) SubmitPassword();
		}
	}
}
