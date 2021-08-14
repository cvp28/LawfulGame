using System;
using System.Net;
using System.Xml;
using System.Collections.Generic;

using Lawful.InputParser;
using Lawful.GameObjects;

using static Lawful.Program;

namespace Lawful
{
    public static class Commands
    {
        public static void SSH(InputQuery Query)
		{
            if (Query.Arguments.Count > 0)
            {
                string[] LoginQuery = Query.Arguments[0].Split('@', StringSplitOptions.RemoveEmptyEntries);

                if (LoginQuery.Length < 2)
				{
					Console.WriteLine("Insufficient parameters");
                    return;
				}

                string Username = LoginQuery[0];
                string Hostname = LoginQuery[1];

                // Error checking
                if (!IPAddress.TryParse(Hostname, out IPAddress TryIP))
                {
					Util.WriteLineColor("Invalid IP address specified", ConsoleColor.Red);
                    return;
                }

                if (Player.ConnectionInfo.PC.Address == TryIP.ToString())
                {
                    Util.WriteLineColor("Already connected to that machine, cannot perform SSH connection", ConsoleColor.Red);
                    return;
                }

                Util.WriteColor($"Trying connection to '{TryIP}' ", ConsoleColor.Yellow);

                Util.BeginSpinningCursorAnimation(new string[4] { "▀", "■", "▄", "■" }, 8, 32, 75, Console.GetCursorPosition());

                Console.Write("- ");

                if (!Computers.HasComputer(TryIP.ToString()))
				{
					Util.WriteLineColor($"could not find a node at the IP address: '{TryIP}'", ConsoleColor.Red);
                    return;
				}

                // Hostname is valid at this point, check Username

                Computer TryPC = Computers.GetComputer(TryIP.ToString());

                if (!TryPC.HasUser(Username))
				{
					Util.WriteLineColor($"node at IP '{TryIP}' does not contain user '{Username}'", ConsoleColor.Red);
                    return;
				}


				Util.WriteLineColor("connected", ConsoleColor.Green);

                // Both are valid at this point, check if user has a password. If so, start login.
                UserAccount TryUser = TryPC.GetUser(Username);
                
                if (TryUser.Password.Length > 0)
				{
                PasswordPrompt:

                    Console.Write("Password: ");
                    string Password = Util.ReadLineSecret();

                    if (Password != TryUser.Password)
                        goto PasswordPrompt;
				}

                Util.WriteLineColor($"Logged in as user '{Username}' at the connected node '{TryIP}'", ConsoleColor.Green);

                Player.ConnectionInfo.PC = TryPC;

                Player.ConnectionInfo.User = TryUser;
                Player.ConnectionInfo.PathNode = Player.ConnectionInfo.PC.GetSystemDrive().Root;
                Player.ConnectionInfo.Drive = Player.ConnectionInfo.PC.GetSystemDrive();

                Events.FireSSHConnect(Player, Computers, Query);
            }
		}

        public static void SwitchUser(InputQuery Query)
		{
            if (Query.Arguments.Count < 1)
			{
				Console.WriteLine("Insufficient parameters");
                return;
			}

            string Username = Query.Arguments[0];

            UserAccount TryAccount = Player.ConnectionInfo.PC.GetUser(Username);

            if (TryAccount is null)
			{
				Console.WriteLine($"Could not find a user by the username '{Username}'");
                return;
			}

            if (TryAccount.Password.Length > 0)
			{
                // Login code
                string Input;

                do
                {
                    Console.Write("Password: ");
                    Input = Util.ReadLineSecret();
                }
                while (Input != TryAccount.Password);

				Console.WriteLine();
			}

            // Actually switch to the user now
            Player.ConnectionInfo.User = TryAccount;

            Util.WriteColor("Success!", ConsoleColor.Green);
            Util.WriteLineColor($" :: Session now active for user '{Player.ConnectionInfo.User.Username}'", ConsoleColor.Yellow);
		}

        public static void Scan(InputQuery Query)
        {
            Console.Write("Scanning... ");

            Util.BeginSpinningCursorAnimation(new char[8] { '|', '/', '-', '\\', '|', '/', '-', '\\' }, 16, 48, 50, Console.GetCursorPosition());

            Util.WriteLineColor("done!", ConsoleColor.Green);
			Console.WriteLine();

            if (Player.ConnectionInfo.PC.ScanResults.Count > 0)
            {
				Console.WriteLine("Responses received:");

                foreach (ScanResult Result in Player.ConnectionInfo.PC.ScanResults)
                    Util.WriteLineColor($"    {Result.Name}@{Result.Address}", ConsoleColor.Yellow);

                
            }
            else
            {
				Console.WriteLine("Scan did not find any PCs open to the internet");
			}
        }

        private static List<IPAddress> ConnectedHosts = new();
        private static List<string> LoggedInUsers = new();

        public static void SecureCopy(InputQuery Query)
		{
            if (Query.Arguments.Count == 0)
			{
				Console.WriteLine("Insufficient arguments");
                return;
			}

            XmlNode Source;
            XmlNode Destination;

            if (Query.Arguments.Count == 1)
            {
                string Arg1 = Query.Arguments[0];

                bool Arg1Succeeded;

                (Arg1Succeeded, Source) = HandleSCPArg(Arg1);

                if (!Arg1Succeeded)
                    return;

                // start copy from remote machine to local machine OR from local machine to elsewhere on local machine

                // If the source is an executable, copy it to bin
                // Else, default to the cwd

                if (Source.Attributes["Command"] is not null)
                    Destination = Player.ConnectionInfo.PC.GetSystemDrive().GetNodeFromPath("/bin");
                else
                    Destination = Player.ConnectionInfo.PathNode;
            }
            else
            {
                string Arg1 = Query.Arguments[0];
                string Arg2 = Query.Arguments[1];

                bool Arg1Succeeded;
                bool Arg2Succeeded;

                (Arg1Succeeded, Source) = HandleSCPArg(Arg1);

                if (!Arg1Succeeded)
                    return;

                (Arg2Succeeded, Destination) = HandleSCPArg(Arg2);

                if (!Arg2Succeeded)
                    return;
            }
            
            Util.WriteDynamicColor($"'{Source.Attributes["Name"].Value}' ({Source.InnerText.Trim().Length}) ".PadRight(30), 10, ConsoleColor.Yellow);

            Util.BeginSpinningCursorAnimation(new char[8] { '|', '/', '-', '\\', '|', '/', '-', '\\' }, 32, 64, 75, Console.GetCursorPosition());
            Util.WriteLineColor("√", ConsoleColor.Green);

            XmlNode CloneOfSource = Source.Clone();

            Destination.AppendChild(CloneOfSource);

            ConnectedHosts.Clear();
            LoggedInUsers.Clear();
		}

        private static (bool, XmlNode) HandleSCPArg(string Arg)
		{
            // Try to parse as a path on the local system first

            XmlNode TryNodeLocal = LocateNode(Arg);

            if (TryNodeLocal is not null)
			{
                return (true, TryNodeLocal);
			}

            // If not, try to parse as a [username]@[hostname]:[path] format

            string[] Format = Arg.Split(':');

            if (Format.Length < 2)
			{
				Console.WriteLine("Invalid remote path format");
				Console.WriteLine("Expected a valid local path or [username]@[hostname]:[path]");
                return (false, null);
			}

            string[] FormatHeader = Format[0].Split('@');

            if (FormatHeader.Length < 2)
			{
				Console.WriteLine("Invalid remote system identifier");
                Console.WriteLine("Expected [username]@[hostname]:[path]");
                return (false, null);
            }

            string Username = FormatHeader[0];
            string Path = Format[1];

            // Check for hostname

            if (!IPAddress.TryParse(FormatHeader[1], out IPAddress Hostname))
			{
				Console.WriteLine("Invalid IP address");
                return (false, null);
			}

            if (!ConnectedHosts.Contains(Hostname))
            {
                Util.WriteColor($"Trying '{Hostname}'... ", ConsoleColor.Yellow);

                Util.BeginSpinningCursorAnimation(new char[8] { '|', '/', '-', '\\', '|', '/', '-', '\\' }, 16, 48, 75, Console.GetCursorPosition());

                if (!Computers.HasComputer(Hostname.ToString()))
                {
                    Util.WriteLineColor($"host at '{Hostname}' did not respond", ConsoleColor.Red);
                    return (false, null);
                }

                Util.WriteLineColor("connected!", ConsoleColor.Green);

                ConnectedHosts.Add(Hostname);
            }

            // Hostname valid, check for username

            Computer Host = Computers.GetComputer(Hostname.ToString());
            
            if (!Host.HasUser(Username))
			{
				Util.WriteLineColor($"Host at '{Hostname}' does not contain user '{Username}'", ConsoleColor.Red);
                return (false, null);
            }
            
            UserAccount User = Host.GetUser(Username);

            if (!LoggedInUsers.Contains(Username))
            {
                // Both are valid, start login for that user if we're not already logged in

                if (User.Password.Length > 0)
                {
                    string Password = String.Empty;

                    do
                    {
                        Console.WriteLine($"Password for '{Username}': ");
                        Password = Util.ReadLineSecret();

                        if (Password == "$cancel")
                            return (false, null);
                    }
                    while (Password != User.Password);

                    LoggedInUsers.Add(Username);
                }
            }

            // Now try to locate the file/directory

            XmlNode TryNode = Host.GetSystemDrive().GetNodeFromPath(Path);

            if (TryNode is null)
			{
				Console.WriteLine($"Could not find '{Path}' on remote host '{Hostname}'");
                return (false, null);
            }

            return (true, TryNode);
		}

        public static void List(InputQuery Query)
        {
            XmlNode NodeToList = Player.ConnectionInfo.PathNode;
            
            if (Query.Flags.Count > 0)
			{
				switch (Query.Flags[0].ToUpper())
				{
                    case "D":
                    case "DISK":
                        for (int i = 0; i < Player.ConnectionInfo.PC.Drives.Count; i++)
                        {
                            PhysicalDrive Disk = Player.ConnectionInfo.PC.Drives[i];

                            Console.Write($"Disk {i} :: ");
							Console.Write($"{ Disk.Root.SelectNodes("Directory").Count} {(Disk.Root.SelectNodes("Directory").Count == 1 ? "folder" : "folders")}, ");
							Console.WriteLine($"{ Disk.Root.SelectNodes("File").Count} {(Disk.Root.SelectNodes("File").Count == 1 ? "file" : "files")} in root");

                            Console.Write("    Type  : ");
                            Util.WriteLineColor(Disk.Type.ToString(), ConsoleColor.Yellow);

                            Console.Write("    Label : ");
                            Util.WriteLineColor(Disk.Label, ConsoleColor.Yellow);

                            if (i != Player.ConnectionInfo.PC.Drives.Count - 1)
							    Console.WriteLine();
                        }
                        return;
				}
			}

            if (Query.Arguments.Count > 0)
            {
                RSIStatus ArgRSIStatus = NodeLocator.GetRSIStatus(Query.Arguments[0]);

                if (ArgRSIStatus > RSIStatus.None)
				{
					Console.WriteLine("Cannot list the contents of a remote directory");
                    return;
				}

                switch (NodeLocator.LocalLocate(Query.Arguments[0], in Player.ConnectionInfo))
				{
                    case XmlNode n:
                        NodeToList = n;
                        break;

                    case XmlNodeList nl:
                        NodeToList = nl[0];
                        break;

                    default:
						Console.WriteLine($"Could not resolve query: '{Query.Arguments[0]}'");
                        break;
				}

            }

			if (NodeToList.Name == "File")
			{
				Console.WriteLine($"Listing details for file '{NodeToList.Attributes["Name"].Value}'");
				Console.WriteLine();

				Console.Write("Path".PadRight(10));
                Util.WriteLineColor(NodeToList.GetPath(), ConsoleColor.Yellow);

				Console.Write("Length".PadRight(10));

                int Length = NodeToList.InnerText.Trim().Length;
                Util.WriteLineColor(Length.ToString() + (Length == 1 ? " character" : " characters"), ConsoleColor.Yellow);

                return;
			}

            Console.WriteLine($"Listing for '{NodeToList.GetPath()}'");
            Console.WriteLine();

            XmlNodeList Folders = NodeToList.SelectNodes("Directory");
            XmlNodeList Files = NodeToList.SelectNodes("File");
            
            if (Folders != null) {

                foreach (XmlNode Folder in Folders)
                    Util.WriteLineColor(Folder.Attributes["Name"].Value, ConsoleColor.Yellow);
			}

            if (Files != null) {

                foreach (XmlNode File in Files)
                    if (File.Attributes["Command"] is not null)
                        Util.WriteLineColor(File.Attributes["Name"].Value, ConsoleColor.Green);
                    else
                        Console.WriteLine(File.Attributes["Name"].Value);
            }
        }

        public static void CD(InputQuery Query)
        {
            // Handle no arguments
            if (Query.Arguments.Count == 0)
            {
				Console.WriteLine("Insufficient parameters");
                return;
			}

            // Handle remote queries
            if (NodeLocator.GetRSIStatus(Query.Arguments[0]) > RSIStatus.None)
			{
				Console.WriteLine("Cannot CD into a remote directory");
                return;
			}

            // Handle query
            switch (NodeLocator.LocalLocate(Query.Arguments[0], in Player.ConnectionInfo, true))
			{
                case XmlNode n:
                    if (n.Name == "File")
					{
						Console.WriteLine("Cannot CD into a file");
                        return;
					}
                    Player.ConnectionInfo.PathNode = n;
                    return;

                case XmlNodeList:
                    Console.WriteLine("Query must resolve to a single directory");
                    return;

                default:
					Console.WriteLine($"Directory '{Query.Arguments[0]}' not found");
                    return;
			}
		}

        public static void Move(InputQuery Query)
		{
            if (Query.Arguments.Count < 2)
			{
				Console.WriteLine("Insufficient parameters");
                return;
			}

            // Test for Remote System Identifiers (RSIs) on both the origin and destination queries
            RSIStatus OriginRSIStatus = NodeLocator.GetRSIStatus(Query.Arguments[0]);
            RSIStatus DestinationRSIStatus = NodeLocator.GetRSIStatus(Query.Arguments[1]);

            // Handle remote queries
            if (OriginRSIStatus > RSIStatus.None || DestinationRSIStatus > RSIStatus.None)
			{
				Console.WriteLine("Move does not support remote queries");
                return;
			}

            // Find each query
            dynamic Origin = NodeLocator.LocalLocate(Query.Arguments[0], in Player.ConnectionInfo);
            dynamic Destination = NodeLocator.LocalLocate(Query.Arguments[1], in Player.ConnectionInfo);

            switch (Destination)
			{
                case XmlNode d:
                    if (d.Name == "File")
					{
						Console.WriteLine("Destination must be a directory");
                        return;
					}
                    switch (Origin)
					{
                        case XmlNode o:

                            d.AppendChild(o);

                            //if (o == Player.ConnectionInfo.PathNode)
                            //    Player.ConnectionInfo.PathNode = d;

                            break;

                        case XmlNodeList nl:
                            int count = nl.Count;

                            for (int i = 0; i < count; i++)
                            {
                                //if (nl[i] == Player.ConnectionInfo.PathNode) // An origin query involving multiple items may include the current directory so we must check for that
                                //    Player.ConnectionInfo.PathNode = d;

                                d.AppendChild(nl[0]);
                            }
                            break;

                        default:
							Console.WriteLine($"Could resolve origin query '{Query.Arguments[0]}', nothing was moved");
                            return;
					}
                    break;

                case XmlNodeList:
					Console.WriteLine("Cannot have more than one destination");
                    return;

                default:
					Console.WriteLine($"Could not resolve destination query '{Query.Arguments[1]}', nothing was moved");
                    return;
			}
		}

		public static XmlNode LocateNode(string Query)
		{
			if (Query.Length == 0) { return null; }

            if (Query[0] == '/')
                return Player.ConnectionInfo.Drive.GetNodeFromPath(Query);
            else
                return Player.ConnectionInfo.PathNode.GetNodeFromPath(Query);
		}

        public static void Remove(InputQuery Query)
		{
            if (Query.Arguments.Count == 0)
			{
				Console.WriteLine("Insufficient parameters");
                return;
			}

            if (NodeLocator.GetRSIStatus(Query.Arguments[0]) > RSIStatus.None)
			{
                Console.WriteLine("Remove does not support remote queries");
                return;
            }

            XmlNode Node = Player.ConnectionInfo.PathNode;

            switch (NodeLocator.LocalLocate(Query.Arguments[0], in Player.ConnectionInfo))
            {
                case XmlNode n:
                    if (n.Name == "Root")
                    {
                        Console.WriteLine("Cannot delete root directory");
                        return;
                    }

                    while (Node.Name != "Root")
                    {
                        if (Node == n)
                        {
                            // error condition
                            Console.WriteLine("Cannot delete a parent of the current working directory");
                            return;
                        }
                        Node = Node.ParentNode;
                    }

                    n.ParentNode.RemoveChild(n);
                    break;

                case XmlNodeList nl:
                    XmlNode Parent = nl[0].ParentNode;

                    while (Node.Name != "Root")
                    {
                        foreach (XmlNode n in nl)
                            if (Node == n)
                            {
                                Console.WriteLine("Cannot delete a parent of the current working directory");
                                return;
                            }

                        Node = Node.ParentNode;
                    }

                    for (int i = nl.Count - 1; i >= 0; i--)
                        Parent.RemoveChild(nl[i]);

                    break;

                default:
                    Console.WriteLine($"Could not resolve query '{Query.Arguments[0]}'");
                    break;
            }
		}

		public static void ChangeRoot(InputQuery Query)
        {
            if (Query.Arguments.Count < 1) {
				Console.WriteLine("Insufficient arguments");
                return;
			}

            PhysicalDrive Disk = Player.ConnectionInfo.PC.GetDisk(Query.Arguments[0]);

            if (Disk is null)
			{
			    Console.WriteLine($"Could not locate a disk by the label '{Query.Arguments[0]}'");
                return;
			}

            Player.ConnectionInfo.Drive = Disk;
            Player.ConnectionInfo.PathNode = Disk.Root;

        }

        public static void Concatenate(InputQuery Query)
        {
            if (Query.Arguments.Count == 0)
			{
				Console.WriteLine("Insufficient parameters");
                return;
			}

            if (NodeLocator.GetRSIStatus(Query.Arguments[0]) > RSIStatus.None)
			{
                Console.WriteLine("Concatenate does not support remote queries");
                return;
            }

            switch (NodeLocator.LocalLocate(Query.Arguments[0], in Player.ConnectionInfo))
            {
                case XmlNode n:
                    if (n.Name != "File")
                    {
                        Console.WriteLine("Query must resolve to a file");
                        return;
                    }
                    Console.WriteLine(n.InnerText.Trim());
                    Events.FireReadFile(Player, Computers, Query, n);
                    break;

                case XmlNodeList:
                    Console.WriteLine("Cannot read more than one file at a time");
                    break;

                default:
                    Console.WriteLine($"Could not find file '{Query.Arguments[0]}'");
                    break;
            }
        }

        public static void Sus() {
            Console.WriteLine($".   ....../#########((#(*,. ,...,,,,,......,..........,**((((/#(/////////////////");
            Console.WriteLine($"........../(######(/*,,.,......,.......................,,***/(#(#(///////////////");
            Console.WriteLine($"          ,(###(//,,,,...,*.............,,....,,,.....,,.,*///((((///////////////");
            Console.WriteLine($"           *,,,,,,..  . .,*,...,,,,*,*****///(///////*****,,,*,,,*///////////////");
            Console.WriteLine($"          .,,,....   ...,,,,,,,*////((((####%%%%%%######(((/*,,,....,**//////////");
            Console.WriteLine($".....  ...,,,.... ...,,,****,,/((((((####%%%%%%%%%%%%%####((((**,,...,/*/////////");
            Console.WriteLine($" .....,*,,,,..   .,,*******/,*(((((########%%%%%&%%%##########(/**,......////////");
            Console.WriteLine($",.***/,,*,,..   ,,****/*****,/((((((#####%%%%%&&&&%%%##########(/*,,.....,///////");
            Console.WriteLine($"*,****//...   .,**///*****,*((((((((#######%%%%%%%%%##########(((/*,,.....,//////");
            Console.WriteLine($"%%%%#(/*,..  ,,************((((############%%%%%&&%%%%%########((/**,....,*///(//");
            Console.WriteLine($"%%%%%%,,,....,**********/(((((((((##(#######%#%%%%%%%%%########(((**,.....*/////(");
            Console.WriteLine($"%%%%%%.,. ...*******//(((((((((((((((((##########%&&%%%#########((*,,.....*(/((/(");
            Console.WriteLine($"%%%%%%*.. ...********/(((((((((((((((((((##((#(#################((/*,.....//(((((");
            Console.WriteLine($"%%%%%%%*....,****,,,,,,,,**///////((((((((((((///*,,,,*,*,***//(##(/,,....(((((((");
            Console.WriteLine($"%%%%%%%#,..,*,.,,*//**,.,,,,,*****/////((////***,,.,,..,********/(((*,..  (((((((");
            Console.WriteLine($"%%%%%%%&. .,,,**/*,.  ..,....,,,*,***(((((/*/**,...***/#/....,*/(&%#(....,(((((((");
            Console.WriteLine($"%%%%%%%&,. ,,***,..,,**,   .*,..,,**/(#%%#(/*,,,,,*,.,,,*/***,/((//##,.///((((/((");
            Console.WriteLine($"%%%%%%#%*,.*//****,,,,*... .....,,,((((#%%(/.,..,,,,*,*/(#(/(##%%%###,,/(/(/(((((");
            Console.WriteLine($"%%%%%%%#*..*/***,,,,,****,,,,,,,,,,/(###%##(((/**,,*,,,**/(#(#((*/(((*,//((((((((");
            Console.WriteLine($"%%%%%%%%*..********,,,,,,,,*//**,,,*(##%%#((((((((((/////((######*/((*.*/(#((((((");
            Console.WriteLine($"%%%%%%%#,..*****///((((((((///******/(###((((((((((((////(((((#####/*///###(((/((");
            Console.WriteLine($"%%%%%%%#...,***///(((((((//********//(#%#(((/((((/////(((##(((((###((*##%%#(((/((");
            Console.WriteLine($"%%%%%%%**,.****////////*****,*****//((((#((((((((((((**,,**///////(/((###%%((((((");
            Console.WriteLine($"%%%%%%%%(,.******/**,,,,**,*******//(((((#(((((((((((///*,,,,**/##*(/(((((//(((((");
            Console.WriteLine($"%%%%%%%%%#.*******.,,,,,,****,*****/(#(((//**///(((((////*,,,,.,/(/*/(((///(/((((");
            Console.WriteLine($"%%%%%%%%%%%(*****.,,..,,,*****,,,,,,,******////(((//***,,...,, ,*/*//(((//(//((((");
            Console.WriteLine($"%%%%%%%%%%%%/***/,/***. ...*****,,,,,,,,*((///****,,,**...,*,,.///*/*/*///////(((");
            Console.WriteLine($"%%%%%%%%%%%(%****/*/****.. ,*****,************((#%(####.,(///,/*(,/*/////////*(((");
            Console.WriteLine($"%%%%%%##%%#,#(****/*(/***,.(%#%#%&/(##(/(&@##&@#&&%&&*,/(////,//*//(((////////(((");
            Console.WriteLine($"###%%%##%##.#%%/***/**/**,,. #%%&&%&&&&(&&@@&&&&&&%..*/((((//(/**/(((///*/////((/");
            Console.WriteLine($"###########*/%%%%/**//*/***,,...,/*%%%%%%&&%%,//,/#***/((((/((//(/////*/*.       ");
            Console.WriteLine($"###########(,%%###(*,*/*/**,,,,../*.,,,,,,,,,,#(&%//*/(##((((*/*/////*/**.    ...");
            Console.WriteLine($"###########%,######((**/******,,*/*,/((#(###((((////(((##((/***/*//**//*,.     ..");
            Console.WriteLine($"############((#(,,**,,,,//****//*,,**/////(//(////((((####(/(((%*#%%(((*..      .");
            Console.WriteLine($"########(/*,**///***./,,,*********/////(((((((((((((((###(///((((.*((#%%%* . ....");
            Console.WriteLine($"((*,,**,****,**///, ***,,,,*****/*///////(/((/(((((#(#(#(*///((((/.#####((##%%%%(");
            Console.WriteLine($"/*,*,,,,***,**///* ****,,,,,***/**/////(((/((((###((#(#*,,**/((((/,######%%%%#((#");
            Console.WriteLine($"**,,,,***,***********,,,*,,,,,,//////(/((((((((#((#((,*//((*,,(((**##############");
            Console.WriteLine($"*,,,,****/****/*/*.,,,,,**,**,,,/*///(((((((((((((((,*////((/,,,((###############");
            Console.WriteLine($"**,,****//*,,,****. *,,,,*****,,,,**///(/(///////,,**/////((((((*#(((############");
        }
    }
}