namespace TouhouButtonWPF
{
	public class User
	{
		public User(string name, string passwordHash, byte[] passwordSalt, long lastLoginTime = 0)
		{
			PasswordHash = passwordHash;
			Name = name;
			LastLoginTime = lastLoginTime;
			PasswordSalt = passwordSalt;
		}

		public string PasswordHash { get; set; }
		public string Name { get; set; }
		public long LastLoginTime { get; set; }
		public byte[] PasswordSalt { get; internal set; }
	}
}