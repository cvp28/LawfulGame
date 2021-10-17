using System;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Collections.Generic;

namespace Lawful.GameLibrary
{
	public class Computer
	{
		[XmlAttribute("Name")]
		public string Name;

		[XmlAttribute("Address")]
		public string Address;

		[XmlElement("ScanResult")]
		public List<ScanResult> ScanResults;

		[XmlElement("Account")]
		public List<UserAccount> Accounts;

		[XmlElement("Root")]
		public XmlNode FileSystemRoot;

		public Computer() { }

		public Computer(string Name, string Address)
		{
			this.Name = Name;
			this.Address = Address;

			ScanResults = new();

			Accounts = new();
			Accounts.Add(new UserAccount("root", String.Empty));
		}

		public Computer(string Name, string Address, string RootPassword)
		{
			this.Name = Name;
			this.Address = Address;

			ScanResults = new();

			Accounts = new();
			Accounts.Add(new("root", RootPassword));
		}

		public UserAccount GetRootUser() => Accounts.FirstOrDefault(account => account.Username == "Root");

		public UserAccount GetUser(string Username) => Accounts.FirstOrDefault(user => user.Username == Username);

		public bool HasUser(string Username) => Accounts.Any(user => user.Username == Username);

		public bool AddUser(UserAccount Account)
		{
			if (Accounts.Any(account => account.Username == Account.Username)) { return false; }

			Accounts.Add(Account);
			return true;
		}

		public bool RemoveUser(string Username)
		{
			if (Username.ToUpper() == "ROOT")
				return false;

			UserAccount Account = Accounts.FirstOrDefault(user => user.Username == Username);

			if (Account is null) { return false; }

			Accounts.Remove(Account);

			return true;
		}
	}
}
