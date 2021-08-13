using System;
using System.Net;
using System.Xml;

using Lawful.GameObjects;
using static Lawful.Program;

namespace Lawful
{
	public enum RSIStatus
	{
		None,           // Format of query does not allow for [username]@[hostname] at start
		InvalidIP,      // Invalid IP address
		NonResolving,   // Valid IP, does not resolve to a host
		Resolves,       // Valid IP, resolves to a host, does not contain specified user
		Complete,       // Valid IP, resolves to a host, computer contains specified user
		Redundant       // Referenced hostname is what we're already connected to
	}

	public static class NodeLocator
	{
		public static RSIStatus GetRSIStatus(string Query)
		{
			if (!Query.Contains(':') || !Query.Contains('@'))
				return RSIStatus.None;

			string[] RemoteSystemIdentifierElements = Query.Split(':', StringSplitOptions.RemoveEmptyEntries)[0].Split('@', StringSplitOptions.RemoveEmptyEntries);

			if (RemoteSystemIdentifierElements.Length != 2)
				return RSIStatus.None;

			string Username = RemoteSystemIdentifierElements[0];
			string Hostname = RemoteSystemIdentifierElements[1];

			bool ValidIP = IPAddress.TryParse(Hostname, out _);
			bool Resolves = Computers.HasComputer(Hostname);

			if (!ValidIP)
				return RSIStatus.InvalidIP;

			if (!Resolves)
				return RSIStatus.NonResolving;

			if (Player.ConnectionInfo.PC.Address == Hostname)
				return RSIStatus.Redundant;

			// At this point, we have determined that the remote system identifier is the correct length and actually represents a valid and non-redundant host
			// Now check user

			bool ValidUser = Computers.GetComputer(Hostname).HasUser(Username);

			if (!ValidUser)
				return RSIStatus.Resolves;

			return RSIStatus.Complete;
		}

		public static dynamic LocalLocate(string Query, in ConnectionInfo ConnectionInfo, bool NoDiskChange = false)
		{
			bool StartAtRoot;
			bool EvaluateDisk = Query.Contains(':') && !NoDiskChange;
			string Path;

			XmlNode Traverser;
			PhysicalDisk TryDisk;

			if (EvaluateDisk)
			{
				string[] QueryElements = Query.Split(':');

				TryDisk = ConnectionInfo.PC.GetDisk(QueryElements[0]);
				Path = QueryElements[1];
				StartAtRoot = true;

				if (TryDisk is null)
					return null;
			}
			else
			{
				TryDisk = ConnectionInfo.Disk;
				Path = Query;
				StartAtRoot = Path[0] == '/';
			}

			if (StartAtRoot)
				Traverser = TryDisk.Root;
			else
				Traverser = ConnectionInfo.PathNode;

			return Locate(Path, Traverser);
		}

		public static dynamic RemoteLocate(string Query)
		{
			string[] QueryElements = Query.Split(':', StringSplitOptions.RemoveEmptyEntries);
			string[] RSIElements = QueryElements[0].Split('@', StringSplitOptions.RemoveEmptyEntries);

			string Username = RSIElements[0];
			string Hostname = RSIElements[1];

			string Path;

			bool EvaluateDisk = QueryElements.Length >= 3;

			Computer TryPC = Computers.GetComputer(Hostname);
			PhysicalDisk TryDisk;

			if (EvaluateDisk)
			{
				TryDisk = TryPC.GetDisk(QueryElements[1]);
				Path = QueryElements[2];

				if (TryDisk is null)
					return null;
			}
			else
			{
				TryDisk = TryPC.GetPrimaryDisk();
				Path = QueryElements[1];
			}

			return Locate(Path, TryDisk.Root);
		}

		public static dynamic Locate(string Path, XmlNode Traverser)
		{
			string[] PathElements = Path.Split('/', StringSplitOptions.RemoveEmptyEntries);

			foreach (string Element in PathElements)
			{
				switch (Element)
				{
					case "*":
						if (Traverser.ChildNodes.Count == 0)
							return null;
						else if (Traverser.ChildNodes.Count == 1)
							return Traverser.FirstChild;
						else
							return Traverser.ChildNodes;

					case "..":
						XmlNode TryParent = Traverser.ParentNode;

						if (Traverser.Name == "Root")
							return null;

						Traverser = TryParent;

						break;

					case ".":
						Traverser = Player.ConnectionInfo.PathNode;
						break;

					default:
						XmlNode TryDirectory = Traverser.SelectSingleNode($"Directory[@Name='{Element}']");
						XmlNode TryFile = Traverser.SelectSingleNode($"File[@Name='{Element}']");

						if (TryDirectory is not null)
							Traverser = TryDirectory;
						else if (TryFile is not null)
							return TryFile;
						else
							return null;

						break;
				}
			}

			return Traverser;
		}
	}
}