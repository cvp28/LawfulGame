using System.Xml;

namespace Lawful.GameLibrary;

public class ConnectionInfo
{
    public string Path { get { return PathNode.GetPath(); } }

    public Computer PC;

    public XmlNode PathNode;

    public UserAccount User;
}
