#r "C:\Users\Carson\source\repos\LawfulGame\Lawful.GameObjects\bin\Release\net5.0\Lawful.GameObjects.dll"

using Lawful.GameObjects;

ComputerStructure cs = new();

Computer main = new("REPLACETHIS", "117.92.201.6", "root");
Computer bedroom_link = new("bedroom_link", "172.116.23.251", "entropy");

UserAccount Player = new("REPLACETHIS", string.Empty);

PhysicalDrive UserSecretsDrive = new(PhysicalDriveType.Regular, Player.Username);
UserSecretsDrive.ImportFileSystemFromDisk(@".\UserSecretsDrive.xml");

Player.SecretsDrive = UserSecretsDrive;

main.AddUser(Player);
main.AddUser(new("www-root", string.Empty));

main.AddDisk(new(PhysicalDriveType.System, "Main", @".\Apollo-PC_Main.xml"));
main.AddDisk(new(PhysicalDriveType.Regular, "Storage", @".\Apollo-PC_Storage.xml"));
//main.AddDisk(new(PhysicalDriveType.UserSpecific, "Konym", @".\Apollo-PC_Main.xml"));

main.ScanResults.Add(new(bedroom_link));

bedroom_link.AddUser(new("jason", "cookies"));

bedroom_link.AddDisk(new(PhysicalDriveType.System, "System", @".\bedroom_link_System.xml"));

cs.AddComputer(main);
cs.AddComputer(bedroom_link);

cs.SerializeToFile(@"..\Content\Story\Lawful\Computers.xml");