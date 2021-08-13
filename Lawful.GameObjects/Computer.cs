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

		[XmlElement("Disk")]
		public List<PhysicalDisk> Disks;

		public Computer() { }

		public Computer(string Name, string Address)
		{
			this.Name = Name;
			this.Address = Address;

			ScanResults = new();

			Accounts = new();
			Accounts.Add(new UserAccount("root", String.Empty));

			Disks = new();
		}

		public Computer(string Name, string Address, string RootPassword)
		{
			this.Name = Name;
			this.Address = Address;

			ScanResults = new();

			Accounts = new();
			Accounts.Add(new("root", RootPassword));

			Disks = new();
		}

		public PhysicalDisk GetPrimaryDisk() => Disks.FirstOrDefault(disk => disk.Type == PhysicalDiskType.Primary);

		public PhysicalDisk GetDisk(string Label) => Disks.FirstOrDefault(disk => disk.Label == Label);

		public bool HasDisk(string Label) => Disks.Any(disk => disk.Label == Label);

		public bool AddDisk(PhysicalDisk Disk)
		{
			if (Disks.Any(disk => disk.Label == Disk.Label)) { return false; }
			if (Disks.Any(disk => disk.Type == PhysicalDiskType.Primary) && Disk.Type == PhysicalDiskType.Primary) { return false; }

			Disks.Add(Disk);
			return true;
		}

		public bool RemoveDisk(string Label)
		{
			PhysicalDisk Disk = Disks.FirstOrDefault(disk => disk.Label == Label);

			if (Disk is null) { return false; }

			Disks.Remove(Disk);

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
