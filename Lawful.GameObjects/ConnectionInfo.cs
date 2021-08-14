using System.Xml;

namespace Lawful.GameObjects
{
    public class ConnectionInfo
    {
        public string Path { get { return PathNode.GetPath(); } }

        public Computer PC;

        public XmlNode PathNode;

        public PhysicalDrive Drive;

        public UserAccount User;
    }
}
