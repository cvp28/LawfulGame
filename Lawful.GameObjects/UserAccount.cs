using System.Xml.Serialization;

namespace Lawful.GameObjects
{
	public class UserAccount
	{
		[XmlAttribute("Username")]
		public string Username;

		[XmlAttribute("Password")]
		public string Password;

		public UserAccount() { }

		public UserAccount(string Username, string Password)
		{
			this.Username = Username;
			this.Password = Password;
		}
	}
}
