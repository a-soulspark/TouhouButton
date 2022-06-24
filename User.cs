namespace TouhouButtonWPF
{
	public class User
	{
		public User(string name, string passwordHash)
		{
			PasswordHash = passwordHash;
			Name = name;
		}

		public string PasswordHash { get; set; }
		public string Name { get; set; }
	}
}