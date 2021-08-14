using System;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Collections.Generic;

namespace Lawful.GameObjects
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

		[XmlElement("Drive")]
		public List<PhysicalDrive> Drives;

		public Computer() { }

		public Computer(string Name, string Address)
		{
			this.Name = Name;
			this.Address = Address;

			ScanResults = new();

			Accounts = new();
			Accounts.Add(new UserAccount("root", String.Empty));

			Drives = new();
		}

		public Computer(string Name, string Address, string RootPassword)
		{
			this.Name = Name;
			this.Address = Address;

			ScanResults = new();

			Accounts = new();
			Accounts.Add(new("root", RootPassword));

			Drives = new();
		}

		public PhysicalDrive GetSystemDrive() => Drives.FirstOrDefault(disk => disk.Type == PhysicalDriveType.System);

		public PhysicalDrive GetDisk(string Label) => Drives.FirstOrDefault(disk => disk.Label == Label);

		public bool HasDisk(string Label) => Drives.Any(disk => disk.Label == Label);

		public bool AddDisk(PhysicalDrive Disk)
		{
			if (Drives.Any(disk => disk.Label == Disk.Label)) { return false; }
			if (Drives.Any(disk => disk.Type == PhysicalDriveType.System) && Disk.Type == PhysicalDriveType.System) { return false; }

			Drives.Add(Disk);
			return true;
		}

		public bool RemoveDisk(string Label)
		{
			PhysicalDrive Disk = Drives.FirstOrDefault(disk => disk.Label == Label);

			if (Disk is null) { return false; }

			Drives.Remove(Disk);

			return true;
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
