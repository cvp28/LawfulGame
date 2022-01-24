using System.Xml;
using System.Xml.Serialization;

namespace Lawful.GameLibrary
{
	public class User
	{
		[XmlIgnore]
		public Computer HomePC;

		[XmlIgnore]
		public UserAccount Account;

		[XmlIgnore]
		public UserSession CurrentSession;

		[XmlIgnore]
		internal string ReferenceHomeAddress;

		[XmlIgnore]
		internal string ReferenceUsername;

		/// <summary>
		/// Used for serialization, use HomePC.Address at runtime instead of this field
		/// </summary>
		[XmlAttribute("HomePCAddress")]
		public string HomePCAddress { get { return HomePC.Address; } set { ReferenceHomeAddress = value; } }

		/// <summary>
		/// Used for serialization, use Account.Username at runtime instead of this field
		/// </summary>
		[XmlAttribute("Username")]
		public string Username { get { return Account.Username; } set { ReferenceUsername = value; } }

		[XmlAttribute("StoryID")]
		public string StoryID { get; set; }

		[XmlAttribute("CurrentMissionID")]
		public string CurrentMissionID { get; set; }

		[XmlAttribute("ProfileName")]
		public string ProfileName { get; set; }

		/// <summary>
		/// Used for serialization, use CurrentSession for all User connection data
		/// </summary>
		[XmlElement("CurrentConnection")]
		public SessionReference CurrentSessionReference;

		public User()
		{
			CurrentSession = new();
			CurrentSessionReference.PlayerReference = this;
		}

		public void SerializeToFile(string Path)
		{
			using FileStream fs = new(Path, FileMode.Create);

			XmlSerializer xs = new(typeof(User));

			xs.Serialize(fs, this);
		}
	
		public static User DeserializeFromFile(string Path)
		{
			if (!File.Exists(Path))
				throw new Exception($"Could not find file referenced by '{Path}'");

			using FileStream fs = new(Path, FileMode.Open);

			XmlSerializer xs = new(typeof(User));

			return xs.Deserialize(fs) as User;
		}
	
		public void PostDeserializationInit(ComputerStructure ComputerStructure)
		{
			HomePC = ComputerStructure.GetComputer(ReferenceHomeAddress);
			Account = HomePC.GetUser(ReferenceUsername);

			CurrentSession.Host = ComputerStructure.GetComputer(CurrentSessionReference.ReferenceAddress);
			CurrentSession.User = CurrentSession.Host.GetUser(CurrentSessionReference.ReferenceUsername);
			CurrentSession.PathNode = CurrentSession.Host.GetNodeFromPath(CurrentSessionReference.ReferencePath);
		}

		// Call ONLY after HomePC and Account have been created
		public void InstantiateSession()
		{
			if (CurrentSession is not null)
				CloseCurrentSession();

			HomePC.TryOpenSession(ProfileName, out CurrentSession);
		}

		public void CloseCurrentSession()
		{
			CurrentSession.Host = null;
			CurrentSession.User = null;
			CurrentSession.PathNode = null;
			CurrentSession = null;
		}
	}
}
