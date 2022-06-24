using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Timers;
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
	/// Lógica interna para TouhouLauncher.xaml
	/// </summary>
	public partial class TouhouLauncher : Window
	{
		private string soundPath = "";
		private string gamePath = "";
		private string gameCfgPath = "";
		private string gameProcessName = "東方紅魔郷";

		public static Dictionary<string, User> Users { get; set; } = new() { { "soulspark", new("soulspark", "soul2398".GetHashCode()) } };
		public User? currentUser;

		public Action<Process> OnGameOpened { get; }
		public Action OnLauncherDismissed { get; }

		public TouhouLauncher(Action<Process> onGameOpened, Action onLauncherCancelled)
		{
			InitializeComponent();

			ReadDatabase();

			using (FileStream stream = File.Open(Directory.GetCurrentDirectory() + "/launcher_config.json", FileMode.Open))
			{
				try
				{
					JsonNode? json = JsonNode.Parse(stream);
					if (json != null)
					{
						Debug.WriteLine($"Configuration loaded: {json}");

						JsonUtils.TryGetUri(json, "iconPath", uri => Icon = new BitmapImage(uri));
						JsonUtils.TryGetValue<string>(json, "gamePath", value => gamePath = value);
						JsonUtils.TryGetValue<string>(json, "gameCfgPath", value => gameCfgPath = value);
						JsonUtils.TryGetValue<string>(json, "soundPath", value => soundPath = value);
						JsonUtils.TryGetValue<string>(json, "playText", value => Play.Content = value);
						JsonUtils.TryGetValue<string>(json, "gameProcessName", value => gameProcessName = value);
						
						JsonUtils.TryGetValue<string>(json, "", value => gamePath = value);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Failed to load configuration file.\n{ex}");
				}
			}

			Timer timer = new(5000);
			timer.Elapsed += (sender, e) => SocialBarLoading.IsEnabled = false;
			timer.Start();

			OnGameOpened = onGameOpened;
			OnLauncherDismissed = onLauncherCancelled;
		}

		private void ReadDatabase()
		{
			Users.Clear();

			var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/TouhouButton/database/";
			Directory.CreateDirectory(path);

			var userFiles = Directory.GetFiles(path);
			foreach (var userFile in userFiles)
			{
				string[] userData = File.ReadAllLines(userFile);
				Users.Add(userFile, new User(userFile, int.Parse(userData[0])));

				UserProfile userProfile = new(userFile, ShowLogInScreen);
				SocialStack.Children.Add(userProfile);
			}

			/* TODO:
			 * potentially shift the code to be more like, professional and stuff.
			 * lol
			 * easier said than done
			 * I mean like, the file reading stuff should be moved from constructor into that other Source...Smth? function
			 * It just looks nicer (and probably behaves nicer too ;)
			 */
		}

		private void TitleBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			Close();
			OnLauncherDismissed();
		}

		private void Play_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				using (var stream = File.Open(soundPath, FileMode.Open))
				{
					var soundPlayer = new SoundPlayer(stream);
					soundPlayer.Play();
				}

				Close();
				MessageBox.Show("↓↑→← Arrow Keys: Move\n" +
					"Z: Shoot\n" +
					"X: Bomb\n" +
					"Shift: Focus\n" +
					"Control: Skip Dialogue\n\n" +
					"🎧Please wear headphones for the best experience.", "Instructions", MessageBoxButton.OK, MessageBoxImage.Information);

				bool fullscreen = Fullscreen.IsChecked == true;
				using (var file = File.Open(gameCfgPath, FileMode.Open, FileAccess.ReadWrite))
				{
					/* DEBUG: print all contents in hexadecimal
					byte[] buffer = new byte[file.Length];
					file.Read(buffer, 0, (int)file.Length);
					StringBuilder builder = new();
					int a = 0;
					foreach (var _byte in buffer) builder.AppendLine($"{(a++).ToString("X")} {_byte.ToString("X")}");
					MessageBox.Show(builder.ToString());

					File.WriteAllText(gameCfgPath + "a", builder.ToString());
					*/

					file.Position = 0x1e;
					file.WriteByte((byte)(fullscreen ? 0 : 1));
				}

				Process vpatchProcess = Process.Start(gamePath);
				vpatchProcess.WaitForExit();

				Process[] touhouProcesses = Process.GetProcessesByName(gameProcessName);
				if (touhouProcesses.Length >= 1) OnGameOpened(touhouProcesses[0]);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Exception caught while playing: {ex}");
			}
		}

		private static readonly List<Expander> expanders = new();

		private void Expander_Expanded(object sender, RoutedEventArgs e)
		{
			foreach (var expander in expanders) if (expander != sender) expander.IsExpanded = false;
		}

		private void Expander_Initialized(object sender, EventArgs e) => expanders.Add((Expander)sender);
/*
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			UserProfile userProfile = new() { Style = ((Button)sender).Style };
			SocialStack.Children.Add(userProfile);
		}*/

		private void LogInButton_Click(object sender, RoutedEventArgs e) => ShowLogInScreen();

		private void ShowLogInScreen(string userName = "")
		{
			LogInPopup logInPopup = new(OnLogIn, userName);
			logInPopup.ShowDialog();
		}

		private void OnLogIn(User user)
		{
			currentUser = user;
			CurrentUserName.Content = user.Name;
			CurrentUserName.Background = new SolidColorBrush(Colors.White);
		}

		private void SocialBarLoading_MouseEnter(object sender, MouseEventArgs e) => SocialBarLoading.IsEnabled = false;

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/TouhouButton/database/";

			foreach (var userName in Users.Keys)
			{
				string[] data = new[] { Users[userName].PasswordHash.ToString() };
				File.WriteAllLines(path + userName, data);
			}
		}

		private void RegisterButton_Click(object sender, RoutedEventArgs e) => ShowLogInScreen();
	}
}
