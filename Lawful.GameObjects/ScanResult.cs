using System.Xml.Serialization;

namespace Lawful.GameLibrary
{
	public class ScanResult
	{
		[XmlAttribute("Name")]
		public string Name;

		[XmlAttribute("Address")]
		public string Address;

		public ScanResult() { }

		public ScanResult(Computer PC)
		{
			this.Name = PC.Name;
			this.Address = PC.Address;
		}

		public ScanResult(string Name, string Address)
		{
			this.Name = Name;
			this.Address = Address;
		}
	}
}
