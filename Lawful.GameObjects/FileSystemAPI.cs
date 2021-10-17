using System;
using System.Xml;
using System.Text;

using Lawful.GameLibrary;

namespace Lawful
{
	// I'll probably use this at some point
	
	public static class FileSystemAPI
	{
		public static string TryReadFile(this Computer Computer)
		{


			return string.Empty;
		}

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

		#endregion
	}
}
