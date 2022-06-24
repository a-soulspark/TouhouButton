namespace TouhouButtonWPF
{
	public class User
	{
		public User(string name, int passwordHash)
		{
			PasswordHash = passwordHash;
			Name = name;
		}

		public int PasswordHash { get; set; }
		public string Name { get; set; }
	}
}