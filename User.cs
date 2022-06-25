namespace TouhouButtonWPF
{
	public class User
	{
		public User(string name, string passwordHash, byte[] passwordSalt, long lastLoginTime = 0, int highScore = 0)
		{
			PasswordHash = passwordHash;
			Name = name;
			LastLoginTime = lastLoginTime;
			PasswordSalt = passwordSalt;
			HighScore = highScore;
		}

		public string PasswordHash { get; set; }
		public string Name { get; set; }
		public long LastLoginTime { get; set; }
		public int HighScore { get; set; } = 0;
		public byte[] PasswordSalt { get; internal set; }
	}
}