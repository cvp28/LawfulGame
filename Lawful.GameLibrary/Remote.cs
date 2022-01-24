using System.Net;

namespace Lawful.GameLibrary;

using static GameSession;

public enum RSIStatus
{
	None,           // Format of query does not allow for [username]@[hostname] at start
	InvalidIP,      // Invalid IP address
	NonResolving,   // Valid IP, does not resolve to a host
	Resolves,       // Valid IP, resolves to a host, does not contain specified user
	Complete,       // Valid IP, resolves to a host, computer contains specified user
	Redundant       // Referenced hostname is what we're already connected to
}

// A class that deals with handing remote systems
public static class Remote
{
	// Function determines if a query validly addresses a remote system and, regardless, returns all the discernable elements of that query using the out strings
	public static RSIStatus TryGetRSI(string Query, out UserAccount User, out Computer Host, out string Path)
	{
		if (!Query.Contains(':') || !Query.Contains('@'))
		{
			User = null;
			Host = null;
			Path = Query;
			return RSIStatus.None;
		}

		string[] QueryElements = Query.Split(':', StringSplitOptions.RemoveEmptyEntries);

		if (QueryElements.Length < 2)
		{
			User = null;
			Host = null;
			Path = Query;
			return RSIStatus.None;
		}

		string[] RSIElements = QueryElements[0].Split('@', StringSplitOptions.RemoveEmptyEntries);

		if (RSIElements.Length != 2)
		{
			User = null;
			Host = null;
			Path = Query;
			return RSIStatus.None;
		}

		// At this point, we know that the string is of a [username]@[hostname]:[path] format

		if (!IPAddress.TryParse(RSIElements[1], out _))
		{
			User = null;
			Host = null;
			Path = QueryElements[1];
			return RSIStatus.InvalidIP;
		}

		Computer TryPC = Computers.GetComputer(RSIElements[1]);

		if (TryPC is null)
		{
			User = null;
			Host = null;
			Path = QueryElements[1];
			return RSIStatus.NonResolving;
		}

		if (Player.CurrentSession.Host.Address == RSIElements[1] && Player.CurrentSession.User.Username == RSIElements[0])
		{
			User = Player.CurrentSession.User;
			Host = TryPC;
			Path = QueryElements[1];
			return RSIStatus.Redundant;
		}

		UserAccount TryUser = TryPC.GetUser(RSIElements[0]);

		if (TryUser is null)
		{
			User = null;
			Host = TryPC;
			Path = QueryElements[1];
			return RSIStatus.Resolves;
		}

		User = TryUser;
		Host = TryPC;
		Path = QueryElements[1];
		return RSIStatus.Complete;
	}

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

		if (Player.CurrentSession.Host.Address == Hostname)
			return RSIStatus.Redundant;

		// At this point, we have determined that the remote system identifier is the correct length and actually represents a valid and non-redundant host
		// Now check user

		bool ValidUser = Computers.GetComputer(Hostname).HasUser(Username);

		if (!ValidUser)
			return RSIStatus.Resolves;

		return RSIStatus.Complete;
	}

	//	// The difference between LocalLocate and RemoteLocate is that...
	//	// LocalLocate will parse the input query as being either of a [disklabel]:[path] format or a [path] format
	//	// RemoteLocate will expect an RSI at the start of the query and therefore is desgined to parse a [username]@[hostname]:[disklabel]:[path] format or a [username]@[hostname]:[path] format with the disk being implied
	//	
	//	public static dynamic LocalLocate(string Query, in ConnectionInfo ConnectionInfo)
	//	{
	//		bool StartAtRoot;
	//		string Path;
	//	
	//		XmlNode Traverser;
	//	
	//		Path = Query;
	//		StartAtRoot = Path[0] == '/';
	//	
	//		if (StartAtRoot)
	//			Traverser = ConnectionInfo.PC.FileSystemRoot;
	//		else
	//			Traverser = ConnectionInfo.PathNode;
	//	
	//		return LocateNode(Path, Traverser);
	//	}
	//	
	//	// RemoteLocate only requires that the PC and User fields of the ConnectionInfo structure being passed in are filled out
	//	// It will automatically determine how to fill out the Drive field based on what is present in the query
	//	// PathNode is not utilized
	//	//
	//	// Yes, this is a shit API.
	//	// Yes, it works.
	//	// No, I do not care.
	//	
	//	public static dynamic RemoteLocate(string Query, in ConnectionInfo ConnectionInfo)
	//	{
	//		string[] QueryElements = Query.Split(':', StringSplitOptions.RemoveEmptyEntries);
	//		string Path;
	//		Path = QueryElements[1];
	//	
	//		return LocateNode(Path, ConnectionInfo.PC.FileSystemRoot);
	//	}
	//	
	//	public static dynamic LocateNode(string Path, XmlNode Traverser)
	//	{
	//		string[] PathElements = Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
	//	
	//		foreach (string Element in PathElements)
	//		{
	//			switch (Element)
	//			{
	//				case "*":
	//					if (Traverser.ChildNodes.Count == 0)
	//						return null;
	//					else if (Traverser.ChildNodes.Count == 1)
	//						return Traverser.FirstChild;
	//					else
	//						return Traverser.ChildNodes;
	//	
	//				case "..":
	//					if (Traverser.Name == "Root")
	//						return null;
	//	
	//					Traverser = Traverser.ParentNode;
	//	
	//					break;
	//	
	//				case ".":
	//					Traverser = Player.CurrentSession.PathNode;
	//					break;
	//	
	//				default:
	//					XmlNode TryDirectory = Traverser.SelectSingleNode($"Directory[@Name='{Element}']");
	//					XmlNode TryFile = Traverser.SelectSingleNode($"File[@Name='{Element}']");
	//	
	//					if (TryDirectory is not null)
	//						Traverser = TryDirectory;
	//					else if (TryFile is not null)
	//						return TryFile;
	//					else
	//						return null;
	//	
	//					break;
	//			}
	//		}
	//	
	//		return Traverser;
	//	}
}