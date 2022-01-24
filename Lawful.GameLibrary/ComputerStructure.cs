using System.Net;
using System.Xml.Serialization;

namespace Lawful.GameLibrary;
	
public class ComputerStructure
{
	[XmlElement("Computer")]
	public List<Computer> Computers;

	public ComputerStructure() { Computers = new(); }

	public Computer GetComputer(string Query)
	{
		if (IPAddress.TryParse(Query, out IPAddress Address))
			return Computers.FirstOrDefault(pc => pc.Address == Address.ToString());
		else
			return Computers.FirstOrDefault(pc => pc.Name == Query);
	}

	public bool HasComputer(string Query)
	{
		if (IPAddress.TryParse(Query, out IPAddress Address))
			return Computers.Any(pc => pc.Address == Address.ToString());
		else
			return Computers.Any(pc => pc.Name == Query);
	}

	public void AddComputer(Computer Item)
	{
		Computers.Add(Item);
	}

	public bool RemoveComputer(string Query)
	{
		Computer PC;

		if (IPAddress.TryParse(Query, out IPAddress Address))
			PC = Computers.FirstOrDefault(pc => pc.Address == Address.ToString());
		else
			PC = Computers.FirstOrDefault(pc => pc.Name == Query);

		if (PC is null)
			return false;

		Computers.Remove(PC);
		return true;
	}

	public void SerializeToFile(string Path)
	{
		using FileStream fs = new(Path, FileMode.Create);

		XmlSerializer xs = new(typeof(ComputerStructure));

		xs.Serialize(fs, this);
	}
		
	public static ComputerStructure DeserializeFromFile(string Path)
	{
		if (!File.Exists(Path))
			throw new Exception($"Could not find file referenced by '{Path}'");
		
		using FileStream fs = new(Path, FileMode.Open);

		XmlSerializer xs = new(typeof(ComputerStructure));

		return xs.Deserialize(fs) as ComputerStructure;
	}
}