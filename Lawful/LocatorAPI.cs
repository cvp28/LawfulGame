﻿using System;
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

		// TODO (Carson): Implement the Locate method which automates determining whether or not to call LocalLocate or RemoteLocate for any given query and handles things like logins internally
		//	public static dynamic Locate(string Query, bool LocalLocateNoDiskChange = false, bool PrintErrors = false)
		//	{
		//		RSIStatus QueryRSIStatus = GetRSIStatus(Query);
		//	
		//		switch (QueryRSIStatus)
		//		{
		//			case RSIStatus.None:
		//				return LocalLocate(Query, in Player.ConnectionInfo, LocalLocateNoDiskChange);
		//	
		//			case RSIStatus.InvalidIP:
		//				if (PrintErrors)
		//					Console.WriteLine("Invalid IP address in query");
		//				return null;
		//	
		//			case RSIStatus.NonResolving:
		//				if (PrintErrors)
		//					Console.WriteLine("Could not locate machine by that IP address");
		//				return null;
		//	
		//			case RSIStatus.Resolves:
		//				if (PrintErrors)
		//					Console.WriteLine("Referenced machine does not contain the specified user");
		//				return null;
		//	
		//			case RSIStatus.Complete:
		//	
		//				//return RemoteLocate(Query, )
		//				return null;
		//	
		//			case RSIStatus.Redundant:
		//				Console.WriteLine("Already connected to that machine");
		//				return null;
		//		}
		//	}

		// The difference between LocalLocate and RemoteLocate is that...
		// LocalLocate will parse the input query as being either of a [disklabel]:[path] format or a [path] format
		// RemoteLocate will expect an RSI at the start of the query and therefore is desgined to parse a [username]@[hostname]:[disklabel]:[path] format or a [username]@[hostname]:[path] format with the disk being implied

		public static dynamic LocalLocate(string Query, in ConnectionInfo ConnectionInfo, bool NoDiskChange = false)
		{
			bool StartAtRoot;
			bool EvaluateDisk = Query.Contains(':') && !NoDiskChange;
			string Path;

			XmlNode Traverser;
			PhysicalDrive TryDisk;

			if (EvaluateDisk)
			{
				string[] QueryElements = Query.Split(':');

				if (ConnectionInfo.User.HasSecretsDrive && QueryElements[0] == ConnectionInfo.User.SecretsDrive.Label)
					TryDisk = ConnectionInfo.User.SecretsDrive;
				else
					TryDisk = ConnectionInfo.PC.GetDisk(QueryElements[0]);

				if (TryDisk is null)
					return null;

				Path = QueryElements[1];
				StartAtRoot = true;
			}
			else
			{
				TryDisk = ConnectionInfo.Drive;
				Path = Query;
				StartAtRoot = Path[0] == '/';
			}

			if (StartAtRoot)
				Traverser = TryDisk.Root;
			else
				Traverser = ConnectionInfo.PathNode;

			return LocateNode(Path, Traverser);
		}

		// RemoteLocate only requires that the PC and User fields of the ConnectionInfo structure being passed in are filled out
		// It will automatically determine how to fill out the Drive field based on what is present in the query
		// PathNode is not utilized
		//
		// Yes, this is a shit API.
		// Yes, it works.
		// No, I do not care.

		public static dynamic RemoteLocate(string Query, in ConnectionInfo ConnectionInfo)
		{
			string[] QueryElements = Query.Split(':', StringSplitOptions.RemoveEmptyEntries);
			string Path;

			bool EvaluateDisk = QueryElements.Length >= 3;

			if (EvaluateDisk)
			{
				if (ConnectionInfo.User.HasSecretsDrive && QueryElements[1] == ConnectionInfo.User.SecretsDrive.Label)
					ConnectionInfo.Drive = ConnectionInfo.User.SecretsDrive;
				else
					ConnectionInfo.Drive = ConnectionInfo.PC.GetDisk(QueryElements[1]);

				if (ConnectionInfo.Drive is null)
					return null;

				Path = QueryElements[2];
			}
			else
			{
				ConnectionInfo.Drive = ConnectionInfo.PC.GetSystemDrive();
				Path = QueryElements[1];
			}

			return LocateNode(Path, ConnectionInfo.Drive.Root);
		}

		public static dynamic LocateNode(string Path, XmlNode Traverser)
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
						if (Traverser.Name == "Root")
							return null;

						Traverser = Traverser.ParentNode;

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