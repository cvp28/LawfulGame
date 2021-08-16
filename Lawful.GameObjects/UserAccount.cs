using System.Xml.Serialization;

namespace Lawful.GameObjects
{
	public class UserAccount
	{
		[XmlAttribute("Username")]
		public string Username;

		[XmlAttribute("Password")]
		public string Password;

		[XmlElement("SecretsDrive")]
		public PhysicalDrive SecretsDrive { get; set; }

		[XmlAttribute("HasSecretsDrive")]
		public bool HasSecretsDrive
		{
			get { return SecretsDrive is not null; }
		}

		public UserAccount() { }

		public UserAccount(string Username, string Password)
		{
			this.Username = Username;
			this.Password = Password;
		}
	}
}
