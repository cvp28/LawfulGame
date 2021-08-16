using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text;

namespace Lawful.GameObjects
{
	public enum PhysicalDriveType
	{
		[XmlEnum("Sytem")]
		System,

		[XmlEnum("Regular")]
		Regular
	}

	public class PhysicalDrive
	{
		[XmlAttribute("Type")]
		public PhysicalDriveType Type;

		[XmlAttribute("Label")]
		public string Label;

		public XmlNode Root;

		public PhysicalDrive() { }

		public PhysicalDrive(PhysicalDriveType Type, string Label)
		{
			this.Type = Type;
			this.Label = Label;

		}

		/// <summary>
		/// Creates a PhysicalDisk with the specified parameters. Recommend wrapping in a try-catch block as XML loading can throw exceptions.
		/// </summary>
		/// <param name="Type">Type of disk</param>
		/// <param name="Label">Label of disk</param>
		/// <param name="PathToFileSystemDocument">Path to the XML document containing the FileSystem for this PhysicalDisk</param>
		public PhysicalDrive(PhysicalDriveType Type, string Label, string PathToFileSystemDocument)
		{
			this.Type = Type;
			this.Label = Label;

			if (!File.Exists(PathToFileSystemDocument)) { throw new Exception($"File does not exist: '{PathToFileSystemDocument}'"); }

			XmlDocument Temp = new();
			Temp.Load(PathToFileSystemDocument);
			Root = Temp.SelectSingleNode("Root");
		}

		/// <summary>
		/// Imports an XML FileSystem from the specified file. Recommend wrapping in a try-catch block as XML loading can throw exceptions
		/// </summary>
		/// <param name="PathToXML">Path to the XML document containing the FileSystem</param>
		public void ImportFileSystemFromDisk(string PathToXML)
		{
			if (!File.Exists(PathToXML)) { throw new Exception($"File does not exist: '{PathToXML}'"); }

			
			XmlDocument Temp = new();
			Temp.Load(PathToXML);
			Root = Temp.SelectSingleNode("Root");
		}


		/// <summary>
		/// Traverses the PhysicalDisk's FileSystem from the root node. Use XmlNode.GetNodeFromPath(string Query) to traverse from a node deeper in the FileSystem.
		/// </summary>
		/// <param name="Query">The path to search for a node</param>
		/// <returns></returns>
		public XmlNode GetNodeFromPath(string Query)
		{
			if (Query.Length == 0) { return null; }

            string[] QueryElements = Query.Split('/', StringSplitOptions.RemoveEmptyEntries);

			XmlNode Traverser = Root;

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
	}

	public static class XmlNodeExtensions
	{
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
	}
}
