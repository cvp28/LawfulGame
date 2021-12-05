using System.Xml.Serialization;

namespace Lawful.GameLibrary
{
	public class Story
	{
		[XmlAttribute("Name")]
		public string? Name { get; set; }

		[XmlAttribute("Start")]
		public string? StartMissionID { get; set; }

		[XmlElement("Mission")]
		public List<StoryMission> Missions;

		public Story() => Missions = new();

		public bool HasMission(string ID) => Missions.Any(m => m.ID == ID);

		public StoryMission GetMission(string ID) => Missions.FirstOrDefault(m => m.ID == ID);

		public void SerializeToFile(string Path)
		{
			using FileStream fs = new(Path, FileMode.Create);

			XmlSerializer xs = new(typeof(Story));

			xs.Serialize(fs, this);
		}

		public static Story DeserializeFromFile(string Path)
		{
			if (!File.Exists(Path))
				throw new Exception($"Could not find file referenced by '{Path}'");

			using FileStream fs = new(Path, FileMode.Open);

			XmlSerializer xs = new(typeof(Story));

			return xs.Deserialize(fs) as Story;
		}
	}
}
