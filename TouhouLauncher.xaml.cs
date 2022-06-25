using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.Json.Nodes;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TouhouButtonWPF
{
	/// <summary>
	/// Lógica interna para TouhouLauncher.xaml
	/// </summary>
	public partial class TouhouLauncher : Window
	{
		private const int FULLSCREEN_CFG_POSITION = 0x1e;
		private string gamePath = "";
		private string gameCfgPath = "";
		private string gameProcessName = "";

		public User? currentUser;

		public Action<Process> OnGameOpened { get; }
		public Action OnLauncherDismissed { get; }

		public TouhouLauncher(Action<Process> onGameOpened, Action onLauncherDismissed)
		{
			InitializeComponent();

			// Load the Social Bar automatically after 5 seconds.
			// Yes, the SocialBarLoading text is a lie. The social bar can easily load instantly.
			// The 5-second delay is a way to overwhelm new users a bit less when they first join.
			Timer timer = new(5000) { AutoReset = false };
			timer.Elapsed += (sender, e) => Dispatcher.Invoke(LoadSocialBar);
			timer.Start();

			OnGameOpened = onGameOpened;
			OnLauncherDismissed = onLauncherDismissed;
		}
		
		// SourceInitialized is the right moment to read configuration files (or so it seems)
		private void Window_SourceInitialized(object sender, EventArgs e) => ReadJsonConfig();

		// Reads the options specified in the launcher_config.json file
		private void ReadJsonConfig()
		{
			using var stream = File.Open(Directory.GetCurrentDirectory() + "/launcher_config.json", FileMode.Open);

			try
			{
				JsonNode? json = JsonNode.Parse(stream);
				if (json != null)
				{
					Debug.WriteLine($"Launcher configuration loaded: {json}");

					TitleBarIcon.Source = Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "./resources/launcher_icon.png", UriKind.Absolute));
					
					JsonUtils.TryGetValue<string>(json, "gameProcessName", value => gameProcessName = value);
					JsonUtils.TryGetValue<string>(json, "gamePath", value => gamePath = value);
					JsonUtils.TryGetValue<string>(json, "gameCfgPath", value => gameCfgPath = value);
					JsonUtils.TryGetValue<string>(json, "playText", value => Play.Content = value);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to load configuration file.\n{ex}");
			}
		}

		// Instantly get rid of the SocialBarLoading text and load the social bar
		private void SocialBarLoading_MouseEnter(object sender, MouseEventArgs e) => LoadSocialBar();

		private void LoadSocialBar()
		{
			// Only load the Social Bar once
			if (SocialBarLoading.IsEnabled)
			{
				SocialBarLoading.IsEnabled = false;
				ReadDatabase();
			}
		}

		// Reads all the files in the %appdata%/TouhouButton/database directory, sorted by Last Modified Time
		// and populates the Social Bar with new users
		private void ReadDatabase()
		{
			var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/TouhouButton/database/";
			Directory.CreateDirectory(path);

			DirectoryInfo info = new(path);
			FileInfo[] userFiles = info.GetFiles().OrderBy(p => p.LastWriteTime).ToArray();

			foreach (var userFile in userFiles) ReadUser(userFile);
		}

		// Reads the data in the user to create a new UserProfile control for it
		private void ReadUser(FileInfo userFile)
		{
			string[] userData = File.ReadAllLines(userFile.FullName);
			var userName = userFile.Name;

			User user = new(userName, userData[0], Convert.FromBase64String(userData[1]), long.Parse(userData[2]));

			UserProfile userProfile = new(this, user, ShowLogInForm);
			userProfile.Notepad.Document.Blocks.Clear();
			foreach (var line in userData[3..^1]) userProfile.Notepad.Document.Blocks.Add(new Paragraph(new Run(line)));

			SocialStack.Children.Add(userProfile);
		}

		// TitleBar Draggable
		private void TitleBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

		// On Close Button pressed, make sure trigger the OnLauncherDismissed event
		private void Close_Click(object sender, RoutedEventArgs e)
		{
			Close();
			OnLauncherDismissed();
		}

		// On Close Button pressed, make sure trigger the OnLauncherDismissed event
		private void Play_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// Get and play the start sound file
				using (var stream = File.Open(TouhouButton.START_SOUND_PATH, FileMode.Open))
				{
					var soundPlayer = new SoundPlayer(stream);
					soundPlayer.Play();
				}

				Close();

				if (currentUser == null) TimedMessageBox.Show("You are playing as a guest user.\n" +
					"Consider registering a username next time: \n" +
					"• shows your score in the launcher\n" +
					"• have your own public notepad for writing your thoughts\n" +
					"+ avoid this message!\n\n" +
					"(if you are already a user, don't forget to log in)",
					title: "Playing as Guest", timer: 3);

				TimedMessageBox.Show("↓↑→← Arrow Keys: Move\n" +
					"Z: Shoot\n" +
					"X: Bomb\n" +
					"Shift: Focus\n" +
					"Control: Skip Dialogue\n\n" +
					"🎧Please wear headphones for the best experience.", "Instructions");

				bool fullscreen = Fullscreen.IsChecked == true;
				using (var file = File.Open(gameCfgPath, FileMode.Open, FileAccess.ReadWrite))
				{
					//DebugHexContents(file); // DEBUG: print all contents in hexadecimal

					// Update the .cfg file with the user's desired Fullscreen option
					file.Position = FULLSCREEN_CFG_POSITION;
					file.WriteByte((byte)(fullscreen ? 0 : 1));
				}

				// Executes vpatch.exe first (auxiliary application which helps Touhou run properly, at 60FPS)
				Process vpatchProcess = Process.Start(gamePath);
				if (vpatchProcess.ProcessName == "vpatch") vpatchProcess.WaitForExit();

				// Once vpatch is closed, automatically search for the Touhou process and notify that to the TouhouButton
				/* TODO: Rewrite this so that Touhou Launcher only hides itself here instead of closing,
				 * waits until the Touhou process has exited,
				 * to finally then notify the TouhouButton.
				 * This will allow the TouhouLauncher to keep updating the user with its new hi-score,
				 * without having to pass all the information about the users to the TouhouButton 👌*/
				Process[] touhouProcesses = Process.GetProcessesByName(gameProcessName);
				if (touhouProcesses.Length >= 1) OnGameOpened(touhouProcesses[0]);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Exception caught while playing: {ex}");
			}
		}

		// DEBUG: print all contents of the stream's file in hexadecimal
		private void DebugHexContents(FileStream file)
		{
			byte[] buffer = new byte[file.Length];
			file.Read(buffer, 0, (int)file.Length);
			StringBuilder builder = new();
			int a = 0;
			foreach (var _byte in buffer) builder.AppendLine($"{(a++).ToString("X")} {_byte.ToString("X")}");
			MessageBox.Show(builder.ToString());

			File.WriteAllText(gameCfgPath + "a", builder.ToString());
		}

		#region Help Expanders
		// Handles ensuring that only one of the Expanders (help boxes) can be expanded at a time. 
		private static readonly List<Expander> helpExpanders = new();

		// When an Expander is expanded, ensure that no other expanders are
		private void Expander_Expanded(object sender, RoutedEventArgs e)
		{
			foreach (var expander in helpExpanders) if (expander != sender) expander.IsExpanded = false;
		}

		// Store all Expanders in a list of Expanders
		private void Expander_Initialized(object sender, EventArgs e) => helpExpanders.Add((Expander)sender);
		#endregion

		private void LogInButton_Click(object sender, RoutedEventArgs e) => ShowLogInForm();

		private void RegisterButton_Click(object sender, RoutedEventArgs e) => ShowRegisterForm();

		// Show the LogInForm, with RegisterMode turned Off
		private void ShowLogInForm(string userName = "")
		{
			LogInForm logInForm = new(this, OnLogIn, false, userName);
			logInForm.ShowDialog();
		}

		// Show the LogInForm, with RegisterMode turned On
		private void ShowRegisterForm()
		{
			LogInForm logInForm = new(this, OnRegister, true, "");
			logInForm.ShowDialog();
		}

		// Called when a new user has successfully signed in, after the LogInForm is closed
		private void OnLogIn(User user)
		{
			currentUser = user;
			CurrentUserName.Content = user.Name;
			CurrentUserName.Background = new SolidColorBrush(Colors.White);
			AccountButtonsGrid.Visibility = Visibility.Collapsed;

			UserProfile? newProfile = null;
			foreach (UserProfile userProfile in SocialStack.Children)
			{
				if (user == userProfile.User)
				{
					userProfile.LogIn();
					newProfile = userProfile;
				}
				userProfile.OnLoggedIn();
			}

			SocialStack.Children.Remove(newProfile);
			SocialStack.Children.Insert(0, newProfile);
		}

		// Called when a new user has just been registered, after the LogInForm is closed
		private void OnRegister(User user)
		{
			SocialStack.Children.Insert(0, new UserProfile(this, user, ShowLogInForm));
			OnLogIn(user);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/TouhouButton/database/";
			Debug.WriteLine($"Ready to write the data to {path}\n\n");

			foreach (UserProfile userProfile in SocialStack.Children)
			{
				WriteUser(path, userProfile);
			}
		}

		private static void WriteUser(string path, UserProfile userProfile)
		{
			string[] data = new string[4];
			data[0] = userProfile.User.PasswordHash;
			data[1] = Convert.ToBase64String(userProfile.User.PasswordSalt);
			data[2] = userProfile.User.LastLoginTime.ToString();
			data[3] = new TextRange(userProfile.Notepad.Document.ContentStart, userProfile.Notepad.Document.ContentEnd).Text
				.Replace(UserProfile.NOTEPAD_PLACEHOLDER, UserProfile.NOTEPAD_PLACEHOLDER_OTHERS);

			File.WriteAllLines(path + userProfile.User.Name, data);
			Debug.WriteLine($"{userProfile.User.Name} : {data.Length}\n");
		}
	}
}
