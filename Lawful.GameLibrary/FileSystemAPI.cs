﻿using System.Xml;
using System.Text;

namespace Lawful.GameLibrary;

// Standard Unix file permissions set
public enum PermissionLevel : int
{
	None,
	Execute,
	Write,
	WriteExecute,
	Read,
	ReadExecute,
	ReadWrite,
	ReadWriteExecute
}

public enum PermissionType : int
{
	Read,
	Write,
	Execute
}

// Standard file permissions set that is not hard to understand
public enum FilePermission : int
{
	Read,		// Able to get the file's contents
	Write,		// Able to modify the file's contents
	Execute		// Able to invoke the file as an executable, whether it is one or not
}

// Standard Windows-inspired directory permissions set
// Why is it Windows-inspired if Linux is so fucking amazing? Because the Windows one is extensible, robust, and EASY TO UNDERSTAND
public enum DirectoryPermission : int
{
	Enter,			// Able to CD into the directory
	List,			// Able to list directory contents
	Modify			// Able to add/remove files/folders from this directory
}

// Might expand on this later
public enum FSNodeType
{
	File,
	Directory
}

public static class FSAPI
{
	public static dynamic Locate(UserSession Session, string Path)
	{
		XmlNode Traverser;

		if (Path[0] == '/')
			Traverser = Session.Host.FileSystemRoot;
		else
			Traverser = Session.PathNode;

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
					Traverser = Session.PathNode;
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

	public static XmlNode LocateFile(UserSession Session, string Path)
	{
		dynamic Result = Locate(Session, Path);

		if (Result == null)
			return null;

		switch (Result)
		{
			case XmlNode n:
				if (n.Name != "File")
					return null;
				else
					return n;

			default:
				return null;
		}
	}

	public static XmlNode LocateDirectory(UserSession Session, string Path)
	{
		dynamic Result = Locate(Session, Path);

		if (Result == null)
			return null;

		switch (Result)
		{
			case XmlNode n:
				if (n.Name == "Root" || n.Name == "Directory")
					return n;
				else
					return null;

			default:
				return null;
		}
	}

	public static bool TryGetNode(UserSession Session, string Path, out dynamic Result)
	{
		Result = Locate(Session, Path);
		return Result != null;
	}

	public static bool TryGetNode(UserSession Session, string Path, FSNodeType Type, out XmlNode Node)
	{
		switch (Type)
		{
			case FSNodeType.File:
				Node = LocateFile(Session, Path);
				return Node != null;

			case FSNodeType.Directory:
				Node = LocateDirectory(Session, Path);
				return Node != null;

			default:
				Node = null;
				break;
		}

		return Node != null;
	}

	public static bool UserHasPermissions(UserSession Session, dynamic Node, params PermissionType[] Permissions)
	{
		switch (Node)
		{
			case XmlNode n:
				return TestNode(Session, n, Permissions);

			case XmlNodeList nl:
				foreach (XmlNode n in nl)
					if (!TestNode(Session, n, Permissions))
						return false;

				return true;

			default:
				return false;
		}

		// I acknowledge that this is a nested function clusterfuck but it is necessary to reduce code size
		static bool TestNode(UserSession Session, XmlNode n, PermissionType[] Permissions)
		{
			var NodePermissions = n.GetPermissionsData();

			if (!NodePermissions.Valid)
				return false;

			if (NodePermissions.Owner == Session.User.Username)
				return true;
			else if (Session.User.Username == "root")
				return TestPermissionLevelForTypes(NodePermissions.RootPerms, Permissions);
			else
				return TestPermissionLevelForTypes(NodePermissions.OtherPerms, Permissions);

			static bool TestPermissionLevelForTypes(PermissionLevel Level, PermissionType[] Permissions)
			{
				foreach (PermissionType p in Permissions)
				{
					switch (p)
					{
						case PermissionType.Read:
							if (!HasReadBit(Level))
								return false;
							break;

						case PermissionType.Write:
							if (!HasWriteBit(Level))
								return false;
							break;

						case PermissionType.Execute:
							if (!HasExecuteBit(Level))
								return false;
							break;
					}
				}

				return true;
			}
		}
	}

	public static bool HasReadBit(PermissionLevel Level) => Level switch
	{
		PermissionLevel.Read => true,
		PermissionLevel.ReadExecute => true,
		PermissionLevel.ReadWrite => true,
		PermissionLevel.ReadWriteExecute => true,
		_ => false
	};

	public static bool HasWriteBit(PermissionLevel Level) => Level switch
	{
		PermissionLevel.Write => true,
		PermissionLevel.WriteExecute => true,
		PermissionLevel.ReadWrite => true,
		PermissionLevel.ReadWriteExecute => true,
		_ => false
	};

	public static bool HasExecuteBit(PermissionLevel Level) => Level switch
	{
		PermissionLevel.Execute => true,
		PermissionLevel.WriteExecute => true,
		PermissionLevel.ReadExecute => true,
		PermissionLevel.ReadWriteExecute => true,
		_ => false
	};

	public static XmlNode GetNodeFromPath(this Computer Computer, string Query)
	{
		if (Query.Length == 0) { return null; }

		string[] QueryElements = Query.Split('/', StringSplitOptions.RemoveEmptyEntries);

		XmlNode Traverser = Computer.FileSystemRoot;

		foreach (string Element in QueryElements)
		{
			if (Traverser.SelectSingleNode($"File[@Name='{Element}']") != null)
			{
				Traverser = Traverser.SelectSingleNode($"File[@Name='{Element}']");
				// If we reach a file, then break because you cannot go further after finding a file
				break;
			}
			else if (Traverser.SelectSingleNode($"Directory[@Name='{Element}']") != null)
				Traverser = Traverser.SelectSingleNode($"Directory[@Name='{Element}']");
			else
				return null;
		}

		return Traverser;
	}

	#region XmlNode Extensions

	public static XmlNode GetNodeFromPath(this XmlNode Node, string Query)
	{
		if (Query.Length == 0) { return null; }

		string[] QueryElements = Query.Split('/', StringSplitOptions.RemoveEmptyEntries);

		XmlNode Traverser = Node;

		foreach (string Element in QueryElements)
		{
			if (Traverser.SelectSingleNode($"File[@Name='{Element}']") is not null)
			{
				Traverser = Traverser.SelectSingleNode($"File[@Name='{Element}']");
				// If we reach a file, then break because you cannot go further after finding a file
				break;
			}
			else if (Traverser.SelectSingleNode($"Directory[@Name='{Element}']") is not null)
			{
				Traverser = Traverser.SelectSingleNode($"Directory[@Name='{Element}']");
			}
			else
			{
				return null;
			}
		}

		return Traverser;
	}

	public static string GetPath(this XmlNode Node)
	{
		StringBuilder Path = new();

		if (Node.Name == "Root") { return "/"; }

		while (Node.Name != "Root")
		{
			Path.Insert(0, $"/{Node.Attributes["Name"].Value}");
			Node = Node.ParentNode;
		}

		return Path.ToString();
	}

	// Function to enumerate permissions data of a directory node
	public static (string Owner, List<DirectoryPermission> RootPermissions, List<DirectoryPermission> OtherPermissions) GetDirectoryPermissionsData(this XmlNode Node)
	{
		// <Directory Perms="root:elm:elm">

		string[] PermissionsElements = Node.Attributes["Perms"].Value.Split(':');

		if (PermissionsElements.Length < 3)
			return (null, null, null);

		string Owner = PermissionsElements[0];
		char[] RootPermissionsElement = PermissionsElements[1].ToArray();
		char[] OtherPermissionsElement = PermissionsElements[2].ToArray();

		List<DirectoryPermission> RootPermissions = ProcessDirectoryPermissions(RootPermissionsElement);
		List<DirectoryPermission> OtherPermissions = ProcessDirectoryPermissions(OtherPermissionsElement);

		return (Owner, RootPermissions, OtherPermissions);

		static List<DirectoryPermission> ProcessDirectoryPermissions(char[] PermissionsElement)
		{
			List<DirectoryPermission> Temp = new();

			foreach (char c in PermissionsElement)
			{
				switch (c)
				{
					case 'e':
					case 'E':
						Temp.Add(DirectoryPermission.Enter);
						break;

					case 'l':
					case 'L':
						Temp.Add(DirectoryPermission.List);
						break;

					case 'm':
					case 'M':
						Temp.Add(DirectoryPermission.Modify);
						break;
				}
			}

			return Temp;
		}
	}

	public static (string Owner, List<FilePermission> RootPermissions, List<FilePermission> OtherPermissions) GetFilePermissionsData(this XmlNode Node)
	{
		// <Directory Perms="root:elm:elm">

		string[] PermissionsElements = Node.Attributes["Perms"].Value.Split(':');

		if (PermissionsElements.Length < 3)
			return (null, null, null);

		string Owner = PermissionsElements[0];
		char[] RootPermissionsElement = PermissionsElements[1].ToArray();
		char[] OtherPermissionsElement = PermissionsElements[2].ToArray();

		List<FilePermission> RootPermissions = ProcessFilePermissions(RootPermissionsElement);
		List<FilePermission> OtherPermissions = ProcessFilePermissions(OtherPermissionsElement);

		return (Owner, RootPermissions, OtherPermissions);

		static List<FilePermission> ProcessFilePermissions(char[] PermissionsElement)
		{
			List<FilePermission> Temp = new();

			foreach (char c in PermissionsElement)
			{
				switch (c)
				{
					case 'r':
					case 'R':
						Temp.Add(FilePermission.Read);
						break;

					case 'w':
					case 'W':
						Temp.Add(FilePermission.Write);
						break;

					case 'e':
					case 'E':
						Temp.Add(FilePermission.Execute);
						break;
				}
			}

			return Temp;
		}
	}

	public static (bool Valid, string Owner, PermissionLevel RootPerms, PermissionLevel OtherPerms) GetPermissionsData(this XmlNode Node)
	{
		string[] PermissionsElements = Node.Attributes["Perms"].Value.Split(':');

		if (PermissionsElements.Length < 2)
			return (false, null, PermissionLevel.None, PermissionLevel.None);

		string Owner = PermissionsElements[0];
		ReadOnlySpan<char> Permissions = PermissionsElements[1].AsSpan();

		if (Permissions.Length != 2)
			return (false, null, PermissionLevel.None, PermissionLevel.None);

		PermissionLevel RootPermissions = (PermissionLevel)int.Parse(Permissions.Slice(0, 1));
		PermissionLevel OtherPermissions = (PermissionLevel)int.Parse(Permissions.Slice(1, 1));

		return (true, Owner, RootPermissions, OtherPermissions);
	}

	#endregion
}
