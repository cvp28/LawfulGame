using System.Xml.Serialization;

namespace Lawful.GameObjects
{
	
	public class StoryMission
	{
		[XmlAttribute("Name")]
		public string Name;

		[XmlAttribute("ID")]
		public string ID;

		[XmlAttribute("AssemblyPath")]
		public string AssemblyPath;
	}
}
