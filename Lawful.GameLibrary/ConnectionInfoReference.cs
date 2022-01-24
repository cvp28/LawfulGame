using System.Xml.Serialization;

namespace Lawful.GameLibrary
{
	public struct ConnectionInfoReference
	{
		[XmlIgnore]
		internal User PlayerReference;

		[XmlIgnore]
		internal string ReferenceAddress;

		[XmlIgnore]
		internal string ReferencePath;

		[XmlIgnore]
		internal string ReferenceUsername;

		// The below fields are for the serializer to use
		// On serialization, it will grab references to the player's current connection via the User object reference at the top
		// On deserialization, they will populate the reference fields above which can be used to intialize the ConnectionInfo object at runtime using User.PostDeserializationInit();

		[XmlAttribute("Address")]
		public string StoredPCAddress
		{
			get { return PlayerReference.CurrentSession.Host.Address; }
			set { ReferenceAddress = value; }
		}


		[XmlAttribute("Path")]
		public string StoredPath
		{
			get { return PlayerReference.CurrentSession.PathNode.GetPath(); }
			set { ReferencePath = value; }
		}


		[XmlAttribute("User")]
		public string StoredUsername
		{
			get { return PlayerReference.CurrentSession.User.Username; }
			set { ReferenceUsername = value; }
		}

	}
}
