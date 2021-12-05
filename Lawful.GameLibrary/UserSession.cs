using System;
using System.Xml;

namespace Lawful.GameLibrary
{
	// This will replace Player.ConnectionInfo as Player.CurrentSession or Player.CurrentShellSession
	public class UserSession
	{
		// The computer that is associated with this UserSession
		public Computer? Host;

		// The user logged in to this session
		public UserAccount? User;

		// The session user's current path node
		public XmlNode? PathNode;

		public static UserSession FromConstituents(Computer Host, UserAccount User) => new()
		{
			Host = Host,
			User = User,
			PathNode = Host.FileSystemRoot
		};
	}
}
